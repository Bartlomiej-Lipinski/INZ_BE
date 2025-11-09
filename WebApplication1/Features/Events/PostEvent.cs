using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Events;

public class PostEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/events", Handle)
            .WithName("PostEvent")
            .WithDescription("Creates a new event for a group")
            .WithTags("Events")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] EventRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} started creating an event in group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            logger.LogWarning("Event creation failed: title is required. User {UserId}, Group {GroupId}, TraceId: {TraceId}",
                userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Event title is required.", traceId));
        }
        
        if (request.StartDate == null && !request.IsAutoScheduled)
        {
            logger.LogWarning("Event creation failed: start date is required for manual events. User {UserId}, Group {GroupId}, TraceId: {TraceId}",
                userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Start date is required for manual events.", traceId));
        }
        
        if (request.IsAutoScheduled)
        {
            if (!request.RangeStart.HasValue || !request.RangeEnd.HasValue || !request.DurationMinutes.HasValue)
            {
                logger.LogWarning("Event creation failed: missing scheduling parameters. User {UserId}, Group {GroupId}, TraceId: {TraceId}",
                    userId, groupId, traceId);
                return Results.BadRequest(ApiResponse<string>.Fail(
                    "For automatic scheduling, range start, range end, and duration are required.", traceId));
            }

            if (request.RangeEnd < request.RangeStart)
            {
                logger.LogWarning("Event creation failed: range end before start. User {UserId}, Group {GroupId}, TraceId: {TraceId}",
                    userId, groupId, traceId);
                return Results.BadRequest(ApiResponse<string>.Fail("Range end cannot be earlier than range start.", traceId));
            }
        }

        if (request is { IsAutoScheduled: false, EndDate: not null, StartDate: not null } 
            && request.EndDate < request.StartDate)
        {
            logger.LogWarning("Event creation failed: end date before start date. User {UserId}, Group {GroupId}, TraceId: {TraceId}",
                userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("End date cannot be earlier than start date.", traceId));
        }

        var newEvent = new Event
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId!,
            EntityType = EntityType.Event,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            IsAutoScheduled = request.IsAutoScheduled,
            RangeStart = request.RangeStart,
            RangeEnd = request.RangeEnd,
            DurationMinutes = request.DurationMinutes,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Events.Add(newEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[PostEvent] User {UserId} created event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, newEvent.Id, groupId, traceId);

        var responseDto = new EventResponseDto
        {
            Id = newEvent.Id,
            GroupId = newEvent.GroupId,
            UserId = newEvent.UserId,
            Title = newEvent.Title,
            Description = newEvent.Description,
            Location = newEvent.Location,
            IsAutoScheduled = newEvent.IsAutoScheduled,
            RangeStart = newEvent.RangeStart?.ToLocalTime(),
            RangeEnd = newEvent.RangeEnd?.ToLocalTime(),
            DurationMinutes = newEvent.DurationMinutes,
            StartDate = newEvent.StartDate?.ToLocalTime(),
            EndDate = newEvent.EndDate?.ToLocalTime(),
            CreatedAt = newEvent.CreatedAt.ToLocalTime()
        };

        logger.LogInformation("User {UserId} successfully created event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, newEvent.Id, groupId, traceId);
        return Results.Ok(ApiResponse<EventResponseDto>.Ok(responseDto, "Event created successfully.", traceId));
    }
}