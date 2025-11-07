using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Timeline.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Timeline;

public class UpdateTimelineEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/timeline/{eventId}", Handle)
            .WithName("UpdateTimelineEvent")
            .WithDescription("Updates an existing timeline event within a group by a member")
            .WithTags("Timeline")
            .RequireAuthorization();
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
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to create a new timeline event. TraceId: {TraceId}", traceId);
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
            logger.LogWarning("User {UserId} attempted to create a new timeline event in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        var timelineEvent =
            await dbContext.TimelineEvents.FirstOrDefaultAsync(te => te.Id == eventId, cancellationToken);
        
        if (timelineEvent == null)
        {
            logger.LogWarning("Timeline event {EventId} not found. TraceId: {TraceId}", eventId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Timeline event not found.", traceId));
        }
        
        if (string.IsNullOrWhiteSpace(request.Title) || request.Date == default)
            return Results.BadRequest(ApiResponse<string>.Fail("Title and date are required.", traceId));

        timelineEvent.Title = request.Title;
        timelineEvent.Date = request.Date;
        timelineEvent.Description = request.Description;
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ApiResponse<string>.Ok("Timeline event updated successfully.", eventId, traceId));
    }
}