using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

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
            .WithOpenApi();
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
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to retrieve polls. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));

        if (group.GroupUsers.All(gu => gu.UserId != userId))
            return Results.Forbid();

        var poll = await dbContext.Polls
            .FirstOrDefaultAsync(p => p.Id == pollId && p.GroupId == groupId, cancellationToken);

        if (poll == null)
        {
            logger.LogWarning("Poll {PollId} not found in group {GroupId}. TraceId: {TraceId}", pollId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Poll not found.", traceId));
        }

        dbContext.Polls.Remove(poll);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} deleted poll {PollId} from group {GroupId}. TraceId: {TraceId}",
            userId, pollId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Poll deleted successfully.", pollId, traceId));
    }
}