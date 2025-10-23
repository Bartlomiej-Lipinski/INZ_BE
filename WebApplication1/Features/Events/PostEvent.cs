using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Events;
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
            .RequireAuthorization()
            .WithOpenApi();
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
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));

        if (group.GroupUsers.All(gu => gu.UserId != userId))
            return Results.Forbid();

        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(ApiResponse<string>.Fail("Event title is required.", traceId));

        if (request.StartDate == default && !request.IsAutoScheduled)
            return Results.BadRequest(ApiResponse<string>.Fail("Start date is required for manual events.",
                traceId));
        
        if (request.IsAutoScheduled)
        {
            if (!request.RangeStart.HasValue || !request.RangeEnd.HasValue || !request.DurationMinutes.HasValue)
                return Results.BadRequest(ApiResponse<string>.Fail(
                    "For automatic scheduling, range start, range end, and duration are required.", traceId));

            if (request.RangeEnd < request.RangeStart)
                return Results.BadRequest(ApiResponse<string>.Fail("Range end cannot be earlier than range start.",
                    traceId));
        }

        if (!request.IsAutoScheduled && request.EndDate.HasValue && request.StartDate != default && request.EndDate < request.StartDate)
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
            RangeStart = request.RangeStart?.ToUniversalTime(),
            RangeEnd = request.RangeEnd?.ToUniversalTime(),
            DurationMinutes = request.DurationMinutes,
            StartDate = request.StartDate.ToUniversalTime(),
            EndDate = request.EndDate?.ToUniversalTime(),
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
            RangeStart = newEvent.RangeStart,
            RangeEnd = newEvent.RangeEnd,
            DurationMinutes = newEvent.DurationMinutes,
            StartDate = newEvent.StartDate,
            EndDate = newEvent.EndDate,
            Status = newEvent.Status,
            CreatedAt = newEvent.CreatedAt
        };

        return Results.Ok(ApiResponse<EventResponseDto>.Ok(responseDto, "Event created successfully.", traceId));
    }
    
    public record EventRequestDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsAutoScheduled { get; set; }
        public DateTime? RangeStart { get; set; }
        public DateTime? RangeEnd { get; set; }
        public int? DurationMinutes { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public EventStatus Status { get; set; }
    }

    public record EventResponseDto
    {
        public string Id { get; set; } = null!;
        public string GroupId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsAutoScheduled { get; set; }
        public DateTime? RangeStart { get; set; }
        public DateTime? RangeEnd { get; set; }
        public int? DurationMinutes { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public EventStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}