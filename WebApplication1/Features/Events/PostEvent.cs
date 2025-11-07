using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events;

public class PostEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/events", Handle)
            .WithName("PostEvent")
            .WithDescription("Creates a new event for a group")
            .WithTags("Events")
            .RequireAuthorization();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] EventRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to create event. TraceId: {TraceId}", traceId);
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

        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to create event in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(ApiResponse<string>.Fail("Event title is required.", traceId));

        if (request.StartDate == null && !request.IsAutoScheduled)
            return Results.BadRequest(ApiResponse<string>.Fail("Start date is required for manual events.",
                traceId));
        
        if (request.IsAutoScheduled)
        {
            if (!request.RangeStart.HasValue || !request.RangeEnd.HasValue || !request.DurationMinutes.HasValue)
                return Results.BadRequest(ApiResponse<string>.Fail(
                    "For automatic scheduling, range start, range end, and duration are required.", traceId));

            if (request.RangeEnd < request.RangeStart)
                return Results.BadRequest(ApiResponse<string>.Fail("Range end cannot be earlier" +
                                                                   " than range start.", traceId));
        }

        if (request is { IsAutoScheduled: false, EndDate: not null, StartDate: not null } 
            && request.EndDate < request.StartDate)
            return Results.BadRequest(ApiResponse<string>.Fail("End date cannot be earlier than start date.",
                traceId));

        var newEvent = new Event
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            IsAutoScheduled = request.IsAutoScheduled,
            RangeStart = request.RangeStart,
            RangeEnd = request.RangeEnd,
            DurationMinutes = request.DurationMinutes,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Events.Add(newEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[PostEvent] User {UserId} created event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, newEvent.Id, groupId, traceId);

        var responseDto = new EventResponseDto
        {
            Id = newEvent.Id,
            GroupId = newEvent.GroupId,
            UserId = newEvent.UserId,
            Title = newEvent.Title,
            Description = newEvent.Description,
            Location = newEvent.Location,
            IsAutoScheduled = newEvent.IsAutoScheduled,
            RangeStart = newEvent.RangeStart?.ToLocalTime(),
            RangeEnd = newEvent.RangeEnd?.ToLocalTime(),
            DurationMinutes = newEvent.DurationMinutes,
            StartDate = newEvent.StartDate?.ToLocalTime(),
            EndDate = newEvent.EndDate?.ToLocalTime(),
            Status = newEvent.Status,
            CreatedAt = newEvent.CreatedAt.ToLocalTime()
        };

        return Results.Ok(ApiResponse<EventResponseDto>.Ok(responseDto, "Event created successfully.", traceId));
    }
}