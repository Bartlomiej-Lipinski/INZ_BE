using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Challenges.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Challenges;

public class UpdateChallenge : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/challenges/{challengeId}", Handle)
            .WithName("UpdateChallenge")
            .WithDescription("Updates a specific challenge in a group")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string challengeId,
        [FromBody] ChallengeRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateChallenge> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Attempting to update challenge in group {GroupId} by user {UserId}. TraceId: {TraceId}",
            groupId, userId, traceId);
        
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingChallenge = await dbContext.Challenges
            .SingleOrDefaultAsync(c => c.Id == challengeId && c.GroupId == groupId, cancellationToken);
        
        if (existingChallenge == null)
        {
            logger.LogWarning("Challenge not found. User {UserId}, ChallengeId {ChallengeId}, Group {GroupId}, TraceId: {TraceId}",
                userId, challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge not found.", traceId));
        }
        
        if (existingChallenge.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to update challenge {ChallengeId} not created by them. TraceId: {TraceId}", 
                userId, challengeId, traceId);
            return Results.Forbid();
        }
        
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
        {
            logger.LogWarning("Challenge update failed: name and description are required. User {UserId}, " +
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
            
        existingChallenge.Name = request.Name;
        existingChallenge.Description = request.Description;
        existingChallenge.StartDate = request.StartDate;
        existingChallenge.EndDate = request.EndDate;
        existingChallenge.GoalUnit = request.GoalUnit;
        existingChallenge.GoalValue = request.GoalValue;
        existingChallenge.IsCompleted = request.IsCompleted ?? false;
        
        var feedItem = await dbContext.GroupFeedItems
            .SingleOrDefaultAsync(f => f.EntityId == challengeId && f.GroupId == groupId, cancellationToken);
        if (feedItem != null)
        {
            feedItem.Title = existingChallenge.Name;
            feedItem.Description = existingChallenge.Description;
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated challenge {ChallengeId} in group {GroupId}. TraceId: {TraceId}",
            userId, challengeId, groupId, traceId);
        
        return Results.Ok(ApiResponse<string>.Ok("Challenge updated successfully.", challengeId, traceId));
    }
}