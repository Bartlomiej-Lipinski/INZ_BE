using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Timeline.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Timeline;

public class GetTimeline : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/timeline", Handle)
            .WithName("GetTimeline")
            .WithDescription("Retrieves all dates for a specific group for its timeline")
            .WithTags("Timeline")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetTimeline> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .ThenInclude(gu => gu.User)
            .Include(g => g.Events)
            .Include(g => g.TimelineEvents)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        var birthdays = group!.GroupUsers
            .Where(gu => gu.User.BirthDate.HasValue)
            .Select(gu => new TimelineEventResponseDto
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"Urodziny {gu.User.UserName}",
                Date = gu.User.BirthDate!.Value.ToDateTime(TimeOnly.FromDateTime(DateTime.Now)).ToLocalTime(),
                Type = EventType.Birthday
            }).ToList();
        
        var events = group.Events
            .Where(e => e.StartDate.HasValue)
            .Select(e => new TimelineEventResponseDto
            {
                Id = Guid.NewGuid().ToString(),
                Title = e.Title,
                Date = e.StartDate!.Value.ToLocalTime(),
                Type = EventType.GroupEvent
            }).ToList();
        
        var customEvents = group.TimelineEvents
            .Select(e => new TimelineEventResponseDto
            {
                Id = Guid.NewGuid().ToString(),
                Title = e.Title,
                Date = e.Date.ToLocalTime(),
                Type = EventType.ImportantDate
            }).ToList();

        var result = birthdays
            .Concat(events)
            .Concat(customEvents)
            .OrderBy(e => e.Date)
            .ToList();
        
        if (result.Count == 0)
            return Results.Ok(ApiResponse<List<TimelineEventResponseDto>>
                .Ok(result, "No timeline found for this group.", traceId));
        
        return Results.Ok(ApiResponse<List<TimelineEventResponseDto>>
            .Ok(result, "Timeline retrieved successfully.", traceId));
    }
}