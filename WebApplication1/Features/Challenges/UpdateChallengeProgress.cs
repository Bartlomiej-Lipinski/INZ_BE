using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Challenges.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Challenges;

public class UpdateChallengeProgress : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/challenges/{challengeId}/progress/{progressId}", Handle)
            .WithName("UpdateChallengeProgress")
            .WithDescription("Updates a specific progress in a challenge")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string challengeId,
        [FromRoute] string progressId,
        [FromBody] ChallengeProgressRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateChallengeProgress> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Attempting to update progress in challenge {ChallengeId} by user {UserId}. TraceId: {TraceId}",
            challengeId, userId, traceId);
        
        var challenge = await dbContext.Challenges
            .SingleOrDefaultAsync(c => c.Id == challengeId && c.GroupId == groupId, cancellationToken);

        if (challenge == null)
        {
            logger.LogWarning("Challenge {ChallengeId} not found in group {GroupId}. TraceId: {TraceId}",
                challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge not found.", traceId));
        }
        
        var participant = await dbContext.ChallengeParticipants
            .SingleOrDefaultAsync(c => c.UserId == userId && c.ChallengeId == challenge.Id, cancellationToken);
        
        if (participant == null)
        {
            logger.LogWarning("Challenge participant {UserId} not found in group {GroupId}. TraceId: {TraceId}",
                challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge participant not found.", traceId));
        }
        
        var progress = await dbContext.ChallengeProgresses
            .SingleOrDefaultAsync(c => c.Id == progressId && c.ChallengeId == challengeId, cancellationToken);
        
        if (progress == null)
        {
            logger.LogWarning("Progress not found. User {UserId}, ProgressId {ProgressId}, Group {GroupId}, TraceId: {TraceId}",
                userId, progressId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Progress not found.", traceId));
        }
        
        if (request.Value <= 0)
        {
            logger.LogWarning("Progress creation failed: value is required. User {UserId}, " +
                              "Group {GroupId}, TraceId: {TraceId}", userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Value is required.",
                traceId));
        }

        var previousValue = progress.Value;
        progress.Description = request.Description;
        progress.Value = request.Value;
        participant.TotalProgress += request.Value - previousValue;
        
        if (participant.TotalProgress >= challenge.GoalValue && !participant.Completed)
        {
            participant.Completed = true;
            participant.CompletedAt = DateTime.UtcNow;

            logger.LogInformation("User {UserId} completed challenge {ChallengeId}. TraceId: {TraceId}",
                userId, challengeId, traceId);

            var allCompleted = await dbContext.ChallengeParticipants
                .Where(p => p.ChallengeId == challenge.Id && p.UserId != userId && p.Completed == false)
                .ToListAsync(cancellationToken);
            
            if (allCompleted.Count == 0)
            {
                challenge.IsCompleted = true;
                logger.LogInformation("Challenge {ChallengeId} marked as completed (all participants finished). " +
                                      "TraceId: {TraceId}", challengeId, traceId);
            }
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} updated progress {ProgressId} in group {GroupId}. TraceId: {TraceId}",
            userId, progressId, groupId, traceId);
        
        return Results.Ok(ApiResponse<string>.Ok("Progress updated successfully.", progressId, traceId));
    }
}