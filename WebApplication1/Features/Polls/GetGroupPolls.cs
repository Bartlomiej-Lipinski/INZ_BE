using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

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
            .AddEndpointFilter<GroupMembershipFilter>();
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
        logger.LogInformation("Fetching polls for group {GroupId}. TraceId: {TraceId}", groupId, traceId);

        var polls = await dbContext.Polls
            .AsNoTracking()
            .Include(p => p.Options)
            .Where(p => p.GroupId == groupId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PollResponseDto
            {
                Id = p.Id,
                CreatedByUserId = p.CreatedByUserId,
                Question = p.Question,
                CreatedAt = p.CreatedAt.ToLocalTime(),
                Options = p.Options.Select(o => new PollOptionDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    VotedUsersIds = o.VotedUsers.Select(u => u.Id).ToList()
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        if (polls.Count == 0)
        {
            logger.LogInformation("No polls found for group {GroupId}. TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<List<PollResponseDto>>.Ok(polls, "No polls found for this group.",
                traceId));
        }

        logger.LogInformation("Retrieved {Count} polls for group {GroupId}. TraceId: {TraceId}", polls.Count, groupId,
            traceId);
        return Results.Ok(ApiResponse<List<PollResponseDto>>.Ok(polls, "Group polls retrieved successfully.",
            traceId));
    }
}