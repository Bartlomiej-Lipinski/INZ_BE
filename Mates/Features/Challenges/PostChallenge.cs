using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Challenges.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities;
using Mates.Infrastructure.Data.Entities.Challenges;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Mates.Shared.Extensions;

namespace Mates.Features.Challenges;

public class PostChallenge : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/challenges", Handle)
            .WithName("PostChallenge")
            .WithDescription("Creates a new challenge for a group")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] ChallengeRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostChallenge> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Attempting to create challenge in group {GroupId} by user {UserId}. TraceId: {TraceId}",
            groupId, userId, traceId);
        
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
        {
            logger.LogWarning("Challenge creation failed: name and description are required. User {UserId}, " +
                              "Group {GroupId}, TraceId: {TraceId}", userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Challenge name and description are required.",
                traceId));
        }

        if (request.EndDate < request.StartDate)
        {
            logger.LogWarning("Challenge creation failed: end date before start date. User {UserId}, Group {GroupId}," +
                              " TraceId: {TraceId}", userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>
                .Fail("Range end cannot be earlier than range start.", traceId));
        }
        
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            
        var challenge = new Challenge
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId!,
            EntityType = EntityType.Challenge,
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            GoalUnit = request.GoalUnit,
            GoalValue = request.GoalValue,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        
        var feedItem = new GroupFeedItem
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId!,
            Type = FeedItemType.Challenge,
            EntityId = challenge.Id,
            StoredFileId = null,
            Title = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };
        
        dbContext.GroupFeedItems.Add(feedItem);
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} created challenge {ChallengeId} in group {GroupId}. TraceId: {TraceId}",
            userId, challenge.Id, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Challenge created successfully.", challenge.Id, traceId));
    }
}