using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Polls;

public class DeletePoll : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/polls/{pollId}", Handle)
            .WithName("DeletePoll")
            .WithDescription("Deletes a specific poll from a group")
            .WithTags("Polls")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string pollId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeletePoll> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Attempting to delete poll {PollId} in group {GroupId} by user {UserId}. TraceId: {TraceId}",
            pollId, groupId, userId, traceId);

        var poll = await dbContext.Polls
            .FirstOrDefaultAsync(p => p.Id == pollId && p.GroupId == groupId, cancellationToken);

        if (poll == null)
        {
            logger.LogWarning("Poll {PollId} not found in group {GroupId}. TraceId: {TraceId}", pollId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Poll not found.", traceId));
        }
        
        var groupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = groupUser?.IsAdmin ?? false;
        if (poll.CreatedByUserId != userId && !isAdmin)
        {
            logger.LogWarning("User {UserId} attempted to delete poll {PollId} they do not own and is not admin. " +
                              "TraceId: {TraceId}", userId, pollId, traceId);
            return Results.Forbid();
        }

        dbContext.Polls.Remove(poll);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} deleted poll {PollId} from group {GroupId}. TraceId: {TraceId}",
            userId, pollId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Poll deleted successfully.", pollId, traceId));
    }
}