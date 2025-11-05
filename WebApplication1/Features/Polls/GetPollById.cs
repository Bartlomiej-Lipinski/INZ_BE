using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Polls;

public class GetPollById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/polls/{pollId}", Handle)
            .WithName("GetPollById")
            .WithDescription("Retrieves a single poll by its ID within a group")
            .WithTags("Polls")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string pollId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetPollById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to retrieve poll. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to retrieve poll in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        var poll = await dbContext.Polls
            .AsNoTracking()
            .Include(p => p.Options)
            .ThenInclude(o => o.VotedUsers)
            .FirstOrDefaultAsync(p => p.Id == pollId && p.GroupId == groupId, cancellationToken);

        if (poll == null)
        {
            logger.LogWarning("Poll {PollId} not found in group {GroupId}. TraceId: {TraceId}", pollId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Poll not found.", traceId));
        }

        var response = new PollResponseDto
        {
            CreatedByUserId = poll.CreatedByUserId,
            Question = poll.Question,
            CreatedAt = poll.CreatedAt.ToLocalTime(),
            Options = poll.Options.Select(o => new PollOptionDto
            {
                Text = o.Text,
                VotedUsers = o.VotedUsers.Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.UserName
                }).ToList()
            }).ToList()
        };
        
        return Results.Ok(ApiResponse<PollResponseDto>.Ok(response, "Poll retrieved successfully.", traceId));
    }
}