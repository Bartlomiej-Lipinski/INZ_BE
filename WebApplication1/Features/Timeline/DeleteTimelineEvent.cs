using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Timeline;

public class DeleteTimelineEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/timeline/{eventId}", Handle)
            .WithName("DeleteTimelineEvent")
            .WithDescription("Deletes a timeline event in a specific group")
            .WithTags("Timeline")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteTimelineEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} deleting event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);

        var timelineEvent = await dbContext.TimelineEvents
            .FirstOrDefaultAsync(te => te.Id == eventId, cancellationToken);
        
        if (timelineEvent == null)
        {
            logger.LogWarning("Timeline event {EventId} not found in group {GroupId}. TraceId: {TraceId}", eventId, 
                groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Timeline event not found.", traceId));
        }
        
        dbContext.TimelineEvents.Remove(timelineEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Timeline event {EventId} deleted successfully. TraceId: {TraceId}", 
            eventId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Timeline event deleted successfully.", eventId, traceId));
    }
}