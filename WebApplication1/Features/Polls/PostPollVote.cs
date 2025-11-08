using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Polls;

public class PostPollVote : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/polls/{pollId}/vote", Handle)
            .WithName("PostPollVote")
            .WithDescription("Creates a new poll vote within a group by a member")
            .WithTags("Polls")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string pollId,
        [FromBody] string optionId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostPollVote> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;
        
        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to vote on a poll in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
        
        var poll = await dbContext.Polls
            .Include(p => p.Options)
            .ThenInclude(o => o.VotedUsers)
            .FirstOrDefaultAsync(p => p.Id == pollId && p.GroupId == groupId, cancellationToken);
        
        if (poll == null)
        {
            logger.LogWarning("Poll {PollId} not found in group {GroupId}. TraceId: {TraceId}", pollId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Poll not found.", traceId));
        }
        
        var option = poll.Options.FirstOrDefault(o => o.Id == optionId);
        if (option == null)
        {
            logger.LogWarning("Option {OptionId} not found in poll {PollId}. TraceId: {TraceId}", optionId, pollId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Option not found in poll.", traceId));
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User {UserId} not found. TraceId: {TraceId}", userId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("User not found.", traceId));
        }
        
        var existingVote = option.VotedUsers.Any(u => u.Id == userId);
        if (existingVote)
        {
            option.VotedUsers.Remove(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(ApiResponse<string>.Ok("Vote removed.", traceId));
        }

        option.VotedUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ApiResponse<string>.Ok("Vote added.", traceId));
    }
}