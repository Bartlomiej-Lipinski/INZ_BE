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

public class ChooseBestDateForEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/events/{eventId}/{suggestionId}/choose-best-date", Handle)
            .WithName("ChooseBestDateForEvent")
            .WithDescription("Chooses the best date for an event based on calculated suggestions")
            .WithTags("Events")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        [FromRoute] string suggestionId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<ChooseBestDateForEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation(
            "User {UserId} attempts to choose best date {SuggestionId} for event {EventId}. TraceId: {TraceId}",
            userId, suggestionId, eventId, traceId);

        var evt = await dbContext.Events
            .Include(e => e.Suggestions)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (evt == null)
        {
            logger.LogWarning("Event {EventId} not found. TraceId: {TraceId}", eventId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }

        evt.StartDate = evt.Suggestions.FirstOrDefault(s => s.Id == suggestionId)?.StartTime;
        evt.EndDate = evt.StartDate?.Add(evt.DurationMinutes.HasValue
            ? TimeSpan.FromMinutes(evt.DurationMinutes.Value)
            : TimeSpan.Zero);
        evt.IsAutoScheduled = false;
        if (evt.StartDate != null)
        {
            evt.Suggestions.Clear();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} set best date for event {EventId} to {StartDate}. TraceId: {TraceId}",
            userId, eventId, evt.StartDate, traceId);
        return Results.Ok(ApiResponse<string>.Ok(null!, "Best date chosen successfully.", traceId));
    }
}