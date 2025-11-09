using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Events;

public class UpdateEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/events/{eventId}", Handle)
            .WithName("UpdateEvent")
            .WithDescription("Updates an existing event in a group")
            .WithTags("Events")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        [FromBody] EventRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        var existingEvent = await dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (existingEvent == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }

        if (existingEvent.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to update event {EventId} not created by them. TraceId: {TraceId}", 
                userId, eventId, traceId);
            return Results.Forbid();
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
            existingEvent.Title = request.Title;

        if (!string.IsNullOrWhiteSpace(request.Description))
            existingEvent.Description = request.Description;

        if (!string.IsNullOrWhiteSpace(request.Location))
            existingEvent.Location = request.Location;
        
        existingEvent.IsAutoScheduled = request.IsAutoScheduled;
        
        if (existingEvent.IsAutoScheduled)
        {
            if (!request.RangeStart.HasValue || !request.RangeEnd.HasValue || !request.DurationMinutes.HasValue)
            {
                return Results.BadRequest(ApiResponse<string>
                    .Fail("For automatic scheduling, range start, range end, and duration are required.",
                        traceId));
            }
        }
        
        switch (request)
        {
            case { RangeStart: not null, RangeEnd: not null } when
                request.RangeEnd.Value < request.RangeStart.Value:
                return Results.BadRequest(ApiResponse<string>
                    .Fail("Range end date cannot be earlier than start date.", traceId));
            case { StartDate: not null, EndDate: not null } when
                request.EndDate.Value < request.StartDate.Value:
                return Results.BadRequest(ApiResponse<string>
                    .Fail("End date cannot be earlier than start date.", traceId));
        }

        existingEvent.RangeStart = request.RangeStart;
        existingEvent.RangeEnd = request.RangeEnd;
        existingEvent.DurationMinutes = request.DurationMinutes;
        existingEvent.StartDate = request.StartDate;
        existingEvent.EndDate = request.EndDate;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);

        var responseDto = new EventResponseDto
        {
            Id = existingEvent.Id,
            GroupId = existingEvent.GroupId,
            UserId = existingEvent.UserId,
            Title = existingEvent.Title,
            Description = existingEvent.Description,
            Location = existingEvent.Location,
            IsAutoScheduled = existingEvent.IsAutoScheduled,
            RangeStart = existingEvent.RangeStart?.ToLocalTime(),
            RangeEnd = existingEvent.RangeEnd?.ToLocalTime(),
            DurationMinutes = existingEvent.DurationMinutes,
            StartDate = existingEvent.StartDate?.ToLocalTime(),
            EndDate = existingEvent.EndDate?.ToLocalTime(),
            CreatedAt = existingEvent.CreatedAt.ToLocalTime()
        };

        return Results.Ok(ApiResponse<EventResponseDto>.Ok(responseDto, "Event updated successfully.", traceId));
    }
}