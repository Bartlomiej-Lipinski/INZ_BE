using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Challenges.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Challenges;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Challenges;

public class PostChallengeProgress : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/challenges/{challengeId}/progress", Handle)
            .WithName("PostChallengeProgress")
            .WithDescription("Creates a new progress for a participant")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string challengeId,
        [FromBody] ChallengeProgressRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostChallengeProgress> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Attempting to add a progress in a challenge {ChallengeId} by user {UserId}. TraceId: {TraceId}",
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

        var progress = new ChallengeProgress
        {
            Id = Guid.NewGuid().ToString(),
            ChallengeId = challengeId,
            UserId = userId!,
            Date = DateTime.UtcNow,
            Description = request.Description,
            Value = request.Value
        };
        
        dbContext.ChallengeProgresses.Add(progress);
        
        participant.TotalProgress += request.Value;
        
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
        
        logger.LogInformation("User {UserId} added progress in a challenge {ChallengeId} in group {GroupId}. TraceId: {TraceId}",
            userId, challengeId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Challenge joined successfully.", progress.Id, traceId));
    }
}