using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Events.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Events;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Events.Availability;

public class PostAvailabilityRange : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/events/{eventId}/availability-range", Handle)
            .WithName("PostAvailabilityRange")
            .WithDescription("Adds an availability range for a user within an event")
            .WithTags("Availabilities")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        [FromBody] List<AvailabilityRangeRequestDto> request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostAvailabilityRange> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation(
            "User {UserId} attempts to post {Count} availability ranges for event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, request.Count, eventId, groupId, traceId);

        var evt = await dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
        {
            logger.LogWarning("Event {EventId} not found in group {GroupId}. TraceId: {TraceId}",
                eventId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }

        var existingRanges = await dbContext.EventAvailabilityRanges
            .Where(ar => ar.EventId == eventId && ar.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existingRanges.Count != 0)
        {
            dbContext.EventAvailabilityRanges.RemoveRange(existingRanges);
            logger.LogInformation("Removed {Count} old availability ranges for user {UserId} in event {EventId}." +
                                  " TraceId: {TraceId}", existingRanges.Count, userId, eventId, traceId);
        }

        var addedRanges = new List<EventAvailabilityRange>();

        foreach (var r in request)
        {
            if (r.AvailableFrom >= r.AvailableTo)
            {
                logger.LogWarning(
                    "Invalid range for user {UserId}: AvailableFrom {From} >= AvailableTo {To}. TraceId: {TraceId}",
                    userId, r.AvailableFrom, r.AvailableTo, traceId);
                return Results.BadRequest(ApiResponse<string>
                    .Fail("AvailableTo must be later than AvailableFrom.", traceId));
            }

            if (evt.IsAutoScheduled)
            {
                if (evt.RangeStart.HasValue && r.AvailableFrom < evt.RangeStart.Value ||
                    evt.RangeEnd.HasValue && r.AvailableTo > evt.RangeEnd.Value)
                {
                    logger.LogWarning("Range {From}-{To} for user {UserId} is outside event range. TraceId: {TraceId}",
                        r.AvailableFrom, r.AvailableTo, userId, traceId);
                    return Results.BadRequest(ApiResponse<string>.Fail("Availability range outside event range.",
                        traceId));
                }
            }

            var hasOverlap = addedRanges.Any(ar =>
                ar.AvailableFrom < r.AvailableTo &&
                ar.AvailableTo > r.AvailableFrom);

            if (hasOverlap)
            {
                logger.LogWarning("User {UserId} submitted overlapping ranges. TraceId: {TraceId}", userId, traceId);
                return Results.BadRequest(
                    ApiResponse<string>.Fail("One or more ranges in the request overlap with each other.", traceId));
            }

            addedRanges.Add(new EventAvailabilityRange
            {
                Id = Guid.NewGuid().ToString(),
                EventId = eventId,
                UserId = userId!,
                AvailableFrom = r.AvailableFrom,
                AvailableTo = r.AvailableTo
            });
        }

        await dbContext.EventAvailabilityRanges.AddRangeAsync(addedRanges, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} added {Count} availability ranges for event {EventId}." +
                              " TraceId: {TraceId}", userId, addedRanges.Count, eventId, traceId);

        var groupMemberIds = await dbContext.GroupUsers
            .Where(gu => gu.GroupId == groupId)
            .Select(gu => gu.UserId)
            .ToListAsync(cancellationToken);

        var usersWithAvailability = await dbContext.EventAvailabilityRanges
            .Where(ar => ar.EventId == eventId)
            .Select(ar => ar.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allUsersSubmitted = groupMemberIds.All(id => usersWithAvailability.Contains(id));

        if (!allUsersSubmitted)
            return Results.Ok(ApiResponse<string>.Ok(null, "Availability ranges added successfully.", traceId));
        logger.LogInformation("All users submitted availability for event {EventId}. Calculating best date.", eventId);

        var cbdeLogger = httpContext.RequestServices
            .GetRequiredService<ILogger<CalculateBestDateForEvent>>();

        await CalculateBestDateForEvent.Handle(
            groupId,
            eventId,
            dbContext,
            currentUser,
            httpContext,
            cbdeLogger,
            cancellationToken
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ApiResponse<string>.Ok(null, "Availability ranges added successfully.", traceId));
    }
}