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

namespace WebApplication1.Features.Events.Availability;

public class PostAvailability : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/events/{eventId}/availability", Handle)
            .WithName("PostAvailability")
            .WithDescription("Creates or updates user's availability for an event")
            .WithTags("Availabilities")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        [FromBody] EventAvailabilityRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostAvailability> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to set availability. TraceId: {TraceId}", traceId);
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
            logger.LogWarning("User {UserId} attempted to set availability in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
        
        var evt = await dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
        {
            logger.LogWarning("Event {EventId} not found in group {GroupId}. TraceId: {TraceId}", eventId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }

        var existing = await dbContext.EventAvailabilities
            .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId, cancellationToken);

        if (existing != null)
        {
            existing.Status = request.Status;
            existing.CreatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("User {UserId} updated availability for event {EventId}. TraceId: {TraceId}",
                userId, eventId, traceId);

            return Results.Ok(ApiResponse<string>.Ok("Availability updated.", traceId));
        }
        
        var availability = new EventAvailability
        {
            EventId = eventId, 
            UserId = userId, 
            Status = request.Status, 
            CreatedAt = DateTime.UtcNow
        };

        dbContext.EventAvailabilities.Add(availability);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} added availability to event {EventId}. TraceId: {TraceId}",
            userId, eventId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Availability added.", traceId));
    }
}