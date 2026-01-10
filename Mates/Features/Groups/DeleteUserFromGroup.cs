using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Groups;

public class DeleteUserFromGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/users/{userId}", Handle)
            .WithName("DeleteUserFromGroup")
            .WithDescription("Deletes a user from a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string userId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        ILogger<DeleteUserFromGroup> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.GetUserId();

        logger.LogInformation(
            "Attempt to remove user {TargetUserId} from group {GroupId} by admin {AdminId}. TraceId: {TraceId}",
            userId, groupId, currentUserId, traceId);

        var currentGroupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = currentGroupUser?.IsAdmin ?? false;
        var isSelfRemoval = currentUserId == userId;

        if (!isAdmin && !isSelfRemoval)
        {
            logger.LogWarning("User {CurrentUserId} is not admin of group {GroupId}. TraceId: {TraceId}",
                currentUserId, groupId, traceId);
            return Results.Forbid();
        }


        var groupUser = await dbContext.GroupUsers
            .SingleOrDefaultAsync(
                gu => gu.GroupId == groupId
                      && gu.UserId == userId
                      && gu.AcceptanceStatus == AcceptanceStatus.Accepted, cancellationToken);

        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} not found in group {GroupId}. TraceId: {TraceId}",
                userId, groupId, traceId);
            return Results.NotFound();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.GroupUsers.Remove(groupUser);

        var hasAnyMembers = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.AcceptanceStatus == AcceptanceStatus.Accepted,
                cancellationToken);

        if (!hasAnyMembers)
        {
            var group = await dbContext.Groups.SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

            if (group != null)
            {
                dbContext.Groups.Remove(group);

                logger.LogInformation("Group {GroupId} removed because last member was deleted. TraceId: {TraceId}",
                    groupId, traceId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("User {UserId} removed from group {GroupId} successfully. TraceId: {TraceId}",
            userId, groupId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("User removed from group successfully.", traceId));
    }
}