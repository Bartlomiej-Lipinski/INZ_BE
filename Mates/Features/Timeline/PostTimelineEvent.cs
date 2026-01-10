using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Timeline.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Mates.Shared.Extensions;

namespace Mates.Features.Timeline;

public class PostTimelineEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/timeline", Handle)
            .WithName("PostTimelineEvent")
            .WithDescription("Creates a new timeline event within a group by a member")
            .WithTags("Timeline")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
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
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} creating event in group {GroupId}. TraceId: {TraceId}", 
            userId, groupId, traceId);
        
        if (string.IsNullOrWhiteSpace(request.Title) || request.Date == default)
        {
            logger.LogWarning("Invalid request by user {UserId} for group {GroupId}. TraceId: {TraceId}", 
                userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Title and date are required.", traceId));
        }
        
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