using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events.Availability;

public class DeleteAvailabilityRange: IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/events/{eventId}/availability-range", Handle)
            .WithName("DeleteAvailabilityRange")
            .WithDescription("Deletes all availability ranges for the current user within an event")
            .WithTags("Availability")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteAvailabilityRange> logger,
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
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));

        if (group.GroupUsers.All(gu => gu.UserId != currentUserId))
            return Results.Forbid();

        var evt = await dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));

        var existingRanges = await dbContext.EventAvailabilityRanges
            .Where(ar => ar.EventId == eventId && ar.UserId == currentUserId)
            .ToListAsync(cancellationToken);

        if (existingRanges.Count == 0)
            return Results.Ok(ApiResponse<string>.Ok("No availability ranges to delete.", traceId));

        dbContext.EventAvailabilityRanges.RemoveRange(existingRanges);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[DeleteAvailabilityRange] User {UserId} deleted {Count} availability ranges for event {EventId}. TraceId: {TraceId}",
            currentUserId, existingRanges.Count, eventId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Availability ranges deleted successfully.", traceId));
    }
}