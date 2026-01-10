using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Challenges.Participants;

public class DeleteChallengeParticipant : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/challenges/{challengeId}/participants/{participantId}", Handle)
            .WithName("DeleteChallengeParticipant")
            .WithDescription("Deletes a specific participant from a challenge")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string challengeId,
        [FromRoute] string participantId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteChallengeParticipant> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} started deletion of challenge participant {ParticipantId} in group {GroupId}. TraceId: {TraceId}",
            userId, participantId, groupId, traceId);

        var challenge = await dbContext.Challenges
            .SingleOrDefaultAsync(c => c.Id == challengeId && c.GroupId == groupId, cancellationToken);
        
        if (challenge == null)
        {
            logger.LogWarning("Challenge {ChallengeId} not found in group {GroupId}. TraceId: {TraceId}", challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge not found.", traceId));
        }
        
        var participant = await dbContext.ChallengeParticipants
            .SingleOrDefaultAsync(c => c.UserId == participantId && c.ChallengeId == challenge.Id, cancellationToken);
        
        if (participant == null)
        {
            logger.LogWarning("Challenge participant {ParticipantId} not found in group {GroupId}. TraceId: {TraceId}",
                participantId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge participant not found.", traceId));
        }

        if (participant.UserId != userId && challenge.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to delete challenge participant {ParticipantId}, " +
                              "but is neither that participant nor challenge author. TraceId: {TraceId}",
                userId, participantId, traceId);
            return Results.Forbid();
        }
        
        dbContext.ChallengeParticipants.Remove(participant);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted challenge participant {ParticipantId} from group {GroupId}. TraceId: {TraceId}",
            userId, participantId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Participant deleted successfully.", challengeId, traceId));
    }
}