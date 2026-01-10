using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Events.Availability;

public class DeleteAvailability : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/events/{eventId}/availability", Handle)
            .WithName("DeleteAvailability")
            .WithDescription("Deletes user's availability for an event")
            .WithTags("Availabilities")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
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
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} attempts to delete availability for event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);
        
        var evt = await dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
        {
            logger.LogWarning("Event {EventId} not found in group {GroupId}. TraceId: {TraceId}", eventId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }
        
        var availability = await dbContext.EventAvailabilities
            .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId, cancellationToken);

        if (availability == null)
        {
            logger.LogWarning("Availability for event {EventId} not found for user {UserId}. TraceId: {TraceId}", 
                eventId, userId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Availability not found.", traceId));
        }
        
        dbContext.EventAvailabilities.Remove(availability);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted availability from event {EventId}." +
                              " TraceId: {TraceId}", userId, eventId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Availability deleted.", traceId));
    }
}