using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events;

public class DeleteEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/events/{eventId}", Handle)
            .WithName("DeleteEvent")
            .WithDescription("Deletes a specific event from a group")
            .WithTags("Events")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to delete event. TraceId: {TraceId}", traceId);
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

        var groupUser = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to delete event in group {GroupId} but is not a member. " +
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

        var isAdmin = groupUser.IsAdmin;
        if (evt.UserId != userId && !isAdmin)
        {
            logger.LogWarning("User {UserId} attempted to delete event {EventId} they do not own and is not admin. " +
                              "TraceId: {TraceId}", userId, eventId, traceId);
            return Results.Forbid();
        }

        dbContext.Events.Remove(evt);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted event {EventId} from group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Event deleted successfully.", eventId, traceId));
    }
}