using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Timeline.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Timeline;

public class PostTimelineEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/timeline", Handle)
            .WithName("PostTimelineEvent")
            .WithDescription("Creates a new timeline event within a group by a member")
            .WithTags("Timeline")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] TimelineEventRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostTimelineEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

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
        
        if (string.IsNullOrWhiteSpace(request.Title) || request.Date == default)
            return Results.BadRequest(ApiResponse<string>.Fail("Title and date are required.", traceId));

        var timelineEvent = new TimelineEvent
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            Title = request.Title,
            Date = request.Date,
            Description = request.Description,
            Type = EventType.ImportantDate
        };

        dbContext.TimelineEvents.Add(timelineEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation(
            "User {UserId} added new timeline event {TimelineEventId} in group {GroupId}. TraceId: {TraceId}",
            userId, timelineEvent.Id, groupId, traceId);
        
        return Results.Ok(ApiResponse<string>
            .Ok("Timeline event created successfully.", timelineEvent.Id, traceId));
    }
}