using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Challenges.Progress;

public class DeleteChallengeProgress : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/challenges/{challengeId}/progress/{progressId}", Handle)
            .WithName("DeleteChallengeProgress")
            .WithDescription("Deletes a specific progress from a challenge")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string challengeId,
        [FromRoute] string progressId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteChallengeProgress> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} started deletion of progress {ProgressId} in group {GroupId}. TraceId: {TraceId}",
            userId, progressId, groupId, traceId);

        var challenge = await dbContext.Challenges
            .SingleOrDefaultAsync(c => c.Id == challengeId && c.GroupId == groupId, cancellationToken);
        
        if (challenge == null)
        {
            logger.LogWarning("Challenge {ChallengeId} not found in group {GroupId}. TraceId: {TraceId}", challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge not found.", traceId));
        }
        
        var participant = await dbContext.ChallengeParticipants
            .SingleOrDefaultAsync(c => c.UserId == userId && c.ChallengeId == challenge.Id, cancellationToken);
        
        if (participant == null)
        {
            logger.LogWarning("Challenge participant {UserId} not found in group {GroupId}. TraceId: {TraceId}",
                userId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge participant not found.", traceId));
        }
        
        var progress = await dbContext.ChallengeProgresses
            .SingleOrDefaultAsync(c => c.Id == progressId 
                                       && c.UserId == userId
                                       && c.ChallengeId == challengeId, cancellationToken);
        
        if (progress == null)
        {
            logger.LogWarning("Progress not found. User {UserId}, ProgressId {ProgressId}, Group {GroupId}, TraceId: {TraceId}",
                userId, progressId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Progress not found.", traceId));
        }
        
        dbContext.ChallengeProgresses.Remove(progress);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted progress {ProgressId} from group {GroupId}. TraceId: {TraceId}",
            userId, progressId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Progress deleted successfully.", challengeId, traceId));
    }
}