using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Events;

public class DeleteEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/events/{eventId}", Handle)
            .WithName("DeleteEvent")
            .WithDescription("Deletes a specific event from a group")
            .WithTags("Events")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
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
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} started deletion of event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);

        var evt = await dbContext.Events
            .SingleOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
        {
            logger.LogWarning("Event {EventId} not found in group {GroupId}. TraceId: {TraceId}", eventId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }

        var groupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = groupUser?.IsAdmin ?? false;
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