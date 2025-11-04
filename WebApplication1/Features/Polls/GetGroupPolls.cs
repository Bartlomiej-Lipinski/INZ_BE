using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Polls;

public class GetGroupPolls : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/polls", Handle)
            .WithName("GetGroupPolls")
            .WithDescription("Retrieves all polls for a specific group")
            .WithTags("Polls")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupPolls> logger,
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
            logger.LogWarning("User {UserId} attempted to retrieve polls in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
        
        var polls = await dbContext.Polls
            .AsNoTracking()
            .Include(p => p.Options)
            .Where(p => p.GroupId == groupId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PollResponseDto
            {
                CreatedByUserId = p.CreatedByUserId,
                Question = p.Question,
                CreatedAt = p.CreatedAt.ToLocalTime(),
                Options = p.Options.Select(o => new PollOptionDto
                {
                    Text = o.Text
                }).ToList()
            })
            .ToListAsync(cancellationToken);
        
        return Results.Ok(ApiResponse<List<PollResponseDto>>.Ok(polls, "Group polls retrieved successfully.",
            traceId));
    }
}