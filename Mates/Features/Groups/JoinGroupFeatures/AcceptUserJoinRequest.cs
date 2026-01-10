using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Groups.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Groups.JoinGroupFeatures;

public class AcceptUserJoinRequest : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/accept-join-request", Handle)
            .WithName("AcceptUserJoinRequest")
            .WithDescription("Accepts a user's join request to a group")
            .WithTags("Groups")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromBody] AcceptUserJoinRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<AcceptUserJoinRequest> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("Processing join request acceptance. " +
                              "GroupId: {GroupId}, UserId: {UserId}, AdminId: {AdminId}. TraceId: {TraceId}",
            request.GroupId, request.UserId, userId, traceId);

        var admin = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == request.GroupId && gu.UserId == userId && gu.IsAdmin,
                cancellationToken);

        if (admin == null)
        {
            logger.LogWarning("User {UserId} is not admin of group {GroupId}. TraceId: {TraceId}",
                userId, request.GroupId, traceId);
            return Results.BadRequest(ApiResponse<string>
                .Fail("Only group admin can accept join requests.", traceId));
        }

        var groupUser = await dbContext.GroupUsers
            .Include(gu => gu.User)
            .FirstOrDefaultAsync(gu => gu.GroupId == request.GroupId && gu.UserId == request.UserId, cancellationToken);

        if (groupUser == null)
        {
            logger.LogWarning("Join request not found. GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}",
                request.GroupId, request.UserId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Join request not found.", traceId));
        }

        if (groupUser.AcceptanceStatus != AcceptanceStatus.Pending)
        {
            logger.LogWarning("Join request is not pending. " +
                              "GroupId: {GroupId}, UserId: {UserId}, Status: {Status}. TraceId: {TraceId}",
                request.GroupId, request.UserId, groupUser.AcceptanceStatus, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Join request is not pending.", traceId));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        groupUser.AcceptanceStatus = AcceptanceStatus.Accepted;
        dbContext.GroupUsers.Update(groupUser);

        var memberDisplayName = request.UserId;
        if (groupUser.User != null)
        {
            var fullName = $"{groupUser.User.Name} {groupUser.User.Surname}".Trim();
            var userName = groupUser.User.UserName;
            var hasFullName = !string.IsNullOrWhiteSpace(fullName);
            var hasUserName = !string.IsNullOrWhiteSpace(userName);

            if (hasFullName && hasUserName)
            {
                memberDisplayName = $"{fullName} (@{userName})";
            }
            else if (hasFullName)
            {
                memberDisplayName = fullName;
            }
            else if (hasUserName)
            {
                memberDisplayName = $"@{userName}";
            }
        }

       
        var feedItem = new GroupFeedItem
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = request.GroupId,
            UserId = request.UserId,
            Type = FeedItemType.Member,
            EntityId = request.UserId,
            Title = $"{memberDisplayName}",
            Description = "Dołączył/a do grupy",
            CreatedAt = DateTime.UtcNow
        };


        dbContext.GroupFeedItems.Add(feedItem);
        var saved = await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (saved > 0)
        {
            logger.LogInformation("Join request accepted successfully. " +
                                  "GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}",
                request.GroupId, request.UserId, traceId);
            return Results.Ok(ApiResponse<string>.Ok("Join request accepted successfully.", null, traceId));
        }

        logger.LogError(
            "Failed to save join request acceptance. GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}",
            request.GroupId, request.UserId, traceId);
        return Results.Json(ApiResponse<string>.Fail("Failed to accept join request.", traceId), statusCode: 500);
    }
}