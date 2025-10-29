using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events;

public class CalculateBestDateForEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{eventId}/calculate-best-date", Handle)
            .WithName("CalculateBestDateForEvent")
            .WithDescription("Calculates the best date for an event based on availabilities")
            .WithTags("Events")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string eventId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<CalculateBestDateForEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        var evt = await dbContext.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (evt == null)
        {
            logger.LogWarning("Event not found. EventId: {EventId}. TraceId: {TraceId}", eventId, traceId);
            return Results.NotFound();
        }

        var isUserInGroup = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == evt.GroupId && gu.UserId == currentUserId, cancellationToken);

        if (!isUserInGroup)
        {
            logger.LogWarning("User {UserId} is not a member of group {GroupId}. TraceId: {TraceId}",
                currentUserId, evt.GroupId, traceId);
            return Results.Forbid();
        }

        var topDates = GetBestDateAndTime(evt);
        foreach (var evtSuggestion in topDates.Select(dateTime => new EventSuggestion
                 {
                     EventId = evt.Id,
                     StartTime = dateTime,
                 }))
        {
            evt.Suggestions.Add(evtSuggestion);
        }
        return Results.Ok(ApiResponse<List<DateTime>>.Ok(topDates, "Top 3 najlepsze daty dodane do sugesti", traceId));
    }


    public static List<DateTime> GetBestDateAndTime(Event ev)
    {
        Dictionary<DateTime, int> dateScores = new();
        Dictionary<DateTime, Dictionary<int, int>> hourScores = new();

        foreach (var availability in ev.AvailabilityRanges)
        {
            for (var date = availability.AvailableFrom.Date;
                 date <= availability.AvailableTo.Date;
                 date = date.AddDays(1))
            {
                if (date < ev.StartDate || date > ev.EndDate)
                    continue;

                if (!dateScores.ContainsKey(date))
                    dateScores[date] = 0;

                dateScores[date]++;
            }
        }

        if (dateScores.Count == 0)
        {
            var fallback = ev.StartDate ?? DateTime.Now;
            return new List<DateTime> { fallback.Date.AddHours(9) };
        }

        var topDates = dateScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .ToList();

        var results = new List<DateTime>();

        foreach (var currentDate in topDates.Select(dateEntry => dateEntry.Key))
        {
            hourScores[currentDate] = new Dictionary<int, int>();

            foreach (var availability in ev.AvailabilityRanges)
            {
                for (var dateTime = availability.AvailableFrom;
                     dateTime <= availability.AvailableTo;
                     dateTime = dateTime.AddHours(1))
                {
                    if (dateTime.Date != currentDate)
                        continue;

                    var hour = dateTime.Hour;
                    if (!hourScores[currentDate].ContainsKey(hour))
                        hourScores[currentDate][hour] = 0;

                    hourScores[currentDate][hour]++;
                }
            }

            var bestHour = 9;
            if (hourScores.TryGetValue(currentDate, out var value) && value.Count != 0)
            {
                var bestHourEntry = value.OrderByDescending(kvp => kvp.Value).First();
                bestHour = bestHourEntry.Key;
            }

            var bestTime = currentDate.Date.AddHours(bestHour);
            results.Add(bestTime);
        }

        return results;
    }
}