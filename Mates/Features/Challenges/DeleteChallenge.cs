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

namespace Mates.Features.Challenges;

public class DeleteChallenge : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/challenges/{challengeId}", Handle)
            .WithName("DeleteChallenge")
            .WithDescription("Deletes a specific challenge from a group")
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
        ILogger<DeleteChallenge> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} started deletion of challenge {ChallengeId} in group {GroupId}. TraceId: {TraceId}",
            userId, challengeId, groupId, traceId);

        var challenge = await dbContext.Challenges
            .SingleOrDefaultAsync(c => c.Id == challengeId && c.GroupId == groupId, cancellationToken);
        
        if (challenge == null)
        {
            logger.LogWarning("Challenge {ChallengeId} not found in group {GroupId}. TraceId: {TraceId}", challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge not found.", traceId));
        }
        
        var groupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = groupUser?.IsAdmin ?? false;
        if (challenge.UserId != userId && !isAdmin)
        {
            logger.LogWarning("User {UserId} attempted to delete challenge {ChallengeId} they do not own and is not admin. " +
                              "TraceId: {TraceId}", userId, challengeId, traceId);
            return Results.Forbid();
        }
        
        var relatedComments = await dbContext.Comments
            .Where(c => c.TargetId == challengeId)
            .ToListAsync(cancellationToken);
        
        if (relatedComments.Count > 0)
        {
            dbContext.Comments.RemoveRange(relatedComments);
            logger.LogInformation("Deleted {Count} comments linked to challenge {ChallengeId}. TraceId: {TraceId}",
                relatedComments.Count, challenge, traceId);
        }
        
        dbContext.Challenges.Remove(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted challenge {ChallengeId} from group {GroupId}. TraceId: {TraceId}",
            userId, challengeId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Challenge deleted successfully.", challengeId, traceId));
    }
}