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
        {
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var isMember = group.GroupUsers.Any(gu => gu.UserId == userId);
        if (!isMember)
        {
            logger.LogWarning("User {UserId} attempted to create event in group {GroupId} they are not a member of." +
                              " TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(ApiResponse<string>.Fail("Event title is required.", traceId));

        if (request.StartDate == default)
            return Results.BadRequest(ApiResponse<string>.Fail("Start date is required.", traceId));

        if (request.EndDate.HasValue && request.EndDate < request.StartDate)
            return Results.BadRequest(ApiResponse<string>.Fail("End date cannot be earlier than start date.", traceId));
        var newEvent = new Event
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            StartDate = request.StartDate.ToUniversalTime(),
            EndDate = request.EndDate?.ToUniversalTime(),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Events.Add(newEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} created event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, newEvent.Id, groupId, traceId);

        var responseDto = new EventResponseDto
        {
            Id = newEvent.Id,
            Title = newEvent.Title,
            Description = newEvent.Description,
            Location = newEvent.Location,
            StartDate = newEvent.StartDate,
            EndDate = newEvent.EndDate,
            CreatedAt = newEvent.CreatedAt,
            UserId = newEvent.UserId,
            GroupId = newEvent.GroupId
        };

        return Results.Ok(ApiResponse<EventResponseDto>.Ok(responseDto, "Event created successfully.", traceId));
    }
    
    public record EventRequestDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public record EventResponseDto
    {
        public string Id { get; set; } = null!;
        public string GroupId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}