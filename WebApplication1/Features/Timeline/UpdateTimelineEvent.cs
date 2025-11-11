using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Timeline.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Timeline;

public class UpdateTimelineEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/timeline/{eventId}", Handle)
            .WithName("UpdateTimelineEvent")
            .WithDescription("Updates an existing timeline event within a group by a member")
            .WithTags("Timeline")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        [FromBody] TimelineEventRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateTimelineEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} updating event {EventId} in group {GroupId}. TraceId: {TraceId}", 
            userId, eventId, groupId, traceId);

        var timelineEvent = await dbContext.TimelineEvents
            .SingleOrDefaultAsync(te => te.Id == eventId, cancellationToken);
        
        if (timelineEvent == null)
        {
            logger.LogWarning("Timeline event {EventId} not found. TraceId: {TraceId}", eventId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Timeline event not found.", traceId));
        }
        
        if (string.IsNullOrWhiteSpace(request.Title) || request.Date == default)
        {
            logger.LogWarning("Invalid update request by user {UserId} for event {EventId}. TraceId: {TraceId}", 
                userId, eventId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Title and date are required.", traceId));
        }
        
        timelineEvent.Title = request.Title;
        timelineEvent.Date = request.Date;
        timelineEvent.Description = request.Description;
        
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Timeline event {EventId} updated successfully by user {UserId} in group {GroupId}. TraceId: {TraceId}", 
            eventId, userId, groupId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Timeline event updated successfully.", eventId, traceId));
    }
}