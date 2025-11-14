using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Challenges;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Challenges.Participants;

public class PostChallengeParticipant : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/challenges/{challengeId}/participants", Handle)
            .WithName("PostChallengeParticipant")
            .WithDescription("Creates a new participant for a challenge")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string challengeId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostChallengeParticipant> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Attempting to join a challenge {ChallengeId} by user {UserId}. TraceId: {TraceId}",
            challengeId, userId, traceId);
        
        var challenge = await dbContext.Challenges
            .SingleOrDefaultAsync(c => c.Id == challengeId && c.GroupId == groupId, cancellationToken);

        if (challenge == null)
        {
            logger.LogWarning("Challenge {ChallengeId} not found in group {GroupId}. TraceId: {TraceId}", challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge not found.", traceId));
        }
        
        var alreadyParticipant = await dbContext.ChallengeParticipants
            .SingleOrDefaultAsync(p => p.ChallengeId == challengeId && p.UserId == userId, cancellationToken);

        if (alreadyParticipant != null)
        {
            logger.LogWarning("User {UserId} already joined challenge {ChallengeId}. TraceId: {TraceId}",
                userId, challengeId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("User already joined this challenge.", traceId));
        }

        var participant = new ChallengeParticipant
        {
            ChallengeId = challengeId,
            UserId = userId!,
            Completed = false,
            TotalProgress = 0,
            JoinedAt = DateTime.UtcNow
        };
        
        dbContext.ChallengeParticipants.Add(participant);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} joined challenge {ChallengeId} in group {GroupId}. TraceId: {TraceId}",
            userId, challengeId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Challenge joined successfully.", participant.UserId, traceId));
    }
}