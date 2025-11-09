using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Timeline.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Timeline;

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