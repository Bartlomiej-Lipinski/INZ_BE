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
    public record BestDateResult(DateTime DateTime, int Score);

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

        var results = topDates
            .Select(d => new BestDateResult(d.date.Add(d.time.TimeOfDay), d.score))
            .ToList();

        return Results.Ok(ApiResponse<List<BestDateResult>>.Ok(results, "Top 3 najlepsze daty", traceId));
    }


    public static List<(DateTime date, DateTime time, int score)> GetBestDateAndTime(Event ev)
    {
        Dictionary<DateTime, int> dateScores = new();
        Dictionary<DateTime, Dictionary<int, int>> hourScores = new();
        var eventAvailabilities = ev.Availabilities;

        // KROK 1: Oblicz punkty tylko dla dat (bez godzin)
        foreach (var availability in ev.AvailabilityRanges)
        {
            var userAvailability = eventAvailabilities.FirstOrDefault(a => a.UserId == availability.UserId);
            int points = userAvailability?.Status switch
            {
                EventAvailabilityStatus.Going => 3,
                EventAvailabilityStatus.NotGoing => 0,
                EventAvailabilityStatus.Maybe => 1,
                _ => 0
            };

            for (DateTime date = availability.AvailableFrom.Date;
                 date <= availability.AvailableTo.Date;
                 date = date.AddDays(1))
            {
                if (date < ev.StartDate || date > ev.EndDate)
                    continue;

                if (!dateScores.ContainsKey(date))
                    dateScores[date] = 0;

                dateScores[date] += points;
            }
        }

        if (!dateScores.Any())
        {
            DateTime fallback = ev.StartDate ?? DateTime.Now;
            return new List<(DateTime, DateTime, int)>
            {
                (fallback, fallback.AddHours(9), 0)
            };
        }

        // Znajdź 3 najlepsze daty
        var topDates = dateScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .ToList();

        var results = new List<(DateTime date, DateTime time, int score)>();

        // KROK 2: Oblicz punkty dla godzin dla każdej z top 3 dat
        foreach (var dateEntry in topDates)
        {
            DateTime currentDate = dateEntry.Key;
            int dateScore = dateEntry.Value;
            hourScores[currentDate] = new Dictionary<int, int>();

            foreach (var availability in ev.AvailabilityRanges)
            {
                var userAvailability = eventAvailabilities.FirstOrDefault(a => a.UserId == availability.UserId);
                int points = userAvailability?.Status switch
                {
                    EventAvailabilityStatus.Going => 3,
                    EventAvailabilityStatus.NotGoing => 0,
                    EventAvailabilityStatus.Maybe => 1,
                    _ => 0
                };

                for (DateTime dateTime = availability.AvailableFrom;
                     dateTime <= availability.AvailableTo;
                     dateTime = dateTime.AddHours(1))
                {
                    if (dateTime.Date != currentDate)
                        continue;

                    int hour = dateTime.Hour;
                    if (!hourScores[currentDate].ContainsKey(hour))
                        hourScores[currentDate][hour] = 0;

                    hourScores[currentDate][hour] += points;
                }
            }

            // Znajdź najlepszą godzinę dla tej daty
            int bestHour = 9;
            if (hourScores.ContainsKey(currentDate) && hourScores[currentDate].Any())
            {
                var bestHourEntry = hourScores[currentDate].OrderByDescending(kvp => kvp.Value).First();
                bestHour = bestHourEntry.Key;
            }

            DateTime bestTime = currentDate.Date.AddHours(bestHour);
            results.Add((currentDate, bestTime, dateScore));
        }

        return results;
    }
}