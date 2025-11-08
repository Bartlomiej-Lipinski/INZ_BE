using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events;

public class GetGroupEvents : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/events", Handle)
            .WithName("GetGroupEvents")
            .WithDescription("Retrieves all events for a specific group")
            .WithTags("Events")
            .RequireAuthorization();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupEvents> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

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
            logger.LogWarning("User {UserId} attempted to get group events, but is not a member. " +
                              "TraceId: {TraceId}", userId, traceId);
            return Results.Forbid();
        }

        var events = await dbContext.Events
            .AsNoTracking()
            .Include(e => e.Availabilities)
            .Where(e => e.GroupId == groupId)
            .OrderBy(e => e.StartDate)
            .Select(e => new EventResponseDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Location = e.Location,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                CreatedAt = e.CreatedAt.ToLocalTime(),
                UserId = e.UserId,
                Availabilities = e.Availabilities.Select(ea => new EventAvailabilityResponseDto
                {
                    UserId = ea.UserId,
                    Status = ea.Status,
                    CreatedAt = ea.CreatedAt.ToLocalTime()
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(ApiResponse<List<EventResponseDto>>.Ok(events, "Group events retrieved successfully.",
            traceId));
    }
}