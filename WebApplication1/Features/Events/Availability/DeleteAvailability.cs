using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events.Availability;

public class DeleteAvailability : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/events/{eventId}/availability", Handle)
            .WithName("DeleteAvailability")
            .WithDescription("Deletes user's availability for an event")
            .WithTags("Availabilities")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteAvailability> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to delete availability. TraceId: {TraceId}", traceId);
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
        
        var availability = await dbContext.EventAvailabilities
            .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == currentUserId, cancellationToken);

        if (availability == null)
        {
            logger.LogWarning("Availability for event {EventId} not found for user {UserId}. TraceId: {TraceId}", 
                eventId, currentUserId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Availability not found.", traceId));
        }
        dbContext.EventAvailabilities.Remove(availability);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted availability from event {EventId}." +
                              " TraceId: {TraceId}", currentUserId, eventId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Availability deleted.", traceId));
    }
}