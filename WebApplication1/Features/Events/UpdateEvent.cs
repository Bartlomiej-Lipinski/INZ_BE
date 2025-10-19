using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events;

public class UpdateEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/events/{eventId}", Handle)
            .WithName("UpdateEvent")
            .WithDescription("Updates an existing event in a group")
            .WithTags("Events")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        [FromBody] UpdateEventRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to update event. TraceId: {TraceId}", traceId);
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
            logger.LogWarning("User {UserId} attempted to update event in group {GroupId} they are not a member." +
                              " TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        var existingEvent = await dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (existingEvent == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }

        if (existingEvent.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to update event {EventId} not created by them. TraceId: {TraceId}", 
                userId, eventId, traceId);
            return Results.Forbid();
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
            existingEvent.Title = request.Title;

        if (!string.IsNullOrWhiteSpace(request.Description))
            existingEvent.Description = request.Description;

        if (!string.IsNullOrWhiteSpace(request.Location))
            existingEvent.Location = request.Location;

        // Compute effective new start and end dates
        var newStartDate = request.StartDate?.ToUniversalTime() ?? existingEvent.StartDate;
        var newEndDate = request.EndDate?.ToUniversalTime() ?? existingEvent.EndDate;

        // Validate that end date is not earlier than start date
        if (newEndDate.HasValue && newEndDate < newStartDate)
        {
            return Results.BadRequest(ApiResponse<string>.Fail("End date cannot be earlier than start date.", traceId));
        }

        existingEvent.StartDate = newStartDate;
        existingEvent.EndDate = newEndDate;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);

        var responseDto = new EventResponseDto
        {
            Id = existingEvent.Id,
            GroupId = existingEvent.GroupId,
            UserId = existingEvent.UserId,
            Title = existingEvent.Title,
            Description = existingEvent.Description,
            Location = existingEvent.Location,
            StartDate = existingEvent.StartDate,
            EndDate = existingEvent.EndDate,
            CreatedAt = existingEvent.CreatedAt
        };

        return Results.Ok(ApiResponse<EventResponseDto>.Ok(responseDto, "Event updated successfully.", traceId));
    }

    public record UpdateEventRequestDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime? StartDate { get; set; }
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