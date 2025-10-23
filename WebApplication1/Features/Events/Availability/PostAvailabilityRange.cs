using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events.Availability;

public class PostAvailabilityRange : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/events/{eventId}/availability-range", Handle)
            .WithName("PostAvailabilityRange")
            .WithDescription("Adds an availability range for a user within an event")
            .WithTags("Availability")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        [FromBody] List<AvailabilityRangeRequestDto> request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostAvailabilityRange> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;
        
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to set availability. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var isMember = group.GroupUsers.Any(gu => gu.UserId == currentUserId);
        if (!isMember)
        {
            logger.LogWarning("User {UserId} is not a member of group {GroupId}. TraceId: {TraceId}",
                currentUserId, groupId, traceId);
            return Results.Forbid();
        }
        
        var evt = await dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
        {
            logger.LogWarning("Event {EventId} not found in group {GroupId}. TraceId: {TraceId}", eventId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }
        
        var existingRanges = await dbContext.EventAvailabilityRanges
            .Where(ar => ar.EventId == eventId && ar.UserId == currentUserId)
            .ToListAsync(cancellationToken);

        if (existingRanges.Count != 0)
        {
            dbContext.EventAvailabilityRanges.RemoveRange(existingRanges);
            logger.LogInformation("Removed {Count} old availability ranges for user {UserId} in event {EventId}. TraceId: {TraceId}",
                existingRanges.Count, currentUserId, eventId, traceId);
        }
        
        var addedRanges = new List<EventAvailabilityRange>();
        
        foreach (var r in request)
        {
            if (r.AvailableFrom >= r.AvailableTo)
                return Results.BadRequest(ApiResponse<string>.Fail("AvailableTo must be later than AvailableFrom.", traceId));

            if (evt.IsAutoScheduled)
            {
                if (evt.RangeStart.HasValue && r.AvailableFrom < evt.RangeStart.Value)
                    return Results.BadRequest(ApiResponse<string>.Fail("Availability starts before event range.", traceId));

                if (evt.RangeEnd.HasValue && r.AvailableTo > evt.RangeEnd.Value)
                    return Results.BadRequest(ApiResponse<string>.Fail("Availability ends after event range.", traceId));
            }

            var hasOverlap = await dbContext.EventAvailabilityRanges
                .AnyAsync(ar =>
                        ar.EventId == eventId &&
                        ar.UserId == currentUserId &&
                        ar.AvailableFrom < r.AvailableTo &&
                        ar.AvailableTo > r.AvailableFrom,
                    cancellationToken);

            if (hasOverlap)
                return Results.BadRequest(ApiResponse<string>.Fail("One or more ranges overlap with existing availability.", traceId));

            addedRanges.Add(new EventAvailabilityRange
            {
                Id = Guid.NewGuid().ToString(),
                EventId = eventId,
                UserId = currentUserId,
                AvailableFrom = r.AvailableFrom.ToUniversalTime(),
                AvailableTo = r.AvailableTo.ToUniversalTime()
            });
        }
        
        await dbContext.EventAvailabilityRanges.AddRangeAsync(addedRanges, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[PostAvailabilityRanges] User {UserId} added {Count} availability ranges for event {EventId}. TraceId: {TraceId}",
            currentUserId, addedRanges.Count, eventId, traceId);

        var responseDtos = addedRanges.Select(r => new AvailabilityRangeResponseDto
        {
            Id = r.Id,
            EventId = r.EventId,
            UserId = r.UserId,
            AvailableFrom = r.AvailableFrom,
            AvailableTo = r.AvailableTo
        }).ToList();

        return Results.Ok(ApiResponse<List<AvailabilityRangeResponseDto>>.Ok(
            responseDtos, "Availability ranges added successfully.", traceId));
    }


    public record AvailabilityRangeRequestDto
    {
        public DateTime AvailableFrom { get; set; }
        public DateTime AvailableTo { get; set; }
    }

    public record AvailabilityRangeResponseDto
    {
        public string Id { get; set; } = null!;
        public string EventId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public DateTime AvailableFrom { get; set; }
        public DateTime AvailableTo { get; set; }
    }
}