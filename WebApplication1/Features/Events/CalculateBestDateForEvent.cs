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

        var (bestDate, bestTime) = GetBestDateAndTime(evt);

        return Results.Ok(ApiResponse<string>.Ok($"{bestTime:G}, {bestDate:D}", "Najlepsza data Obliczona", traceId));
    }


    public static (DateTime bestDate, DateTime bestTime) GetBestDateAndTime(Event ev)
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
            return (fallback, fallback.AddHours(9));
        }

        // Znajdź najlepszą datę
        DateTime bestDate = dateScores.OrderByDescending(kvp => kvp.Value).First().Key;

        bool isWeekend = bestDate.DayOfWeek == DayOfWeek.Saturday || bestDate.DayOfWeek == DayOfWeek.Sunday;
        int bonusStartHour = isWeekend ? 12 : 17;

        // KROK 2: Oblicz punkty dla godzin tylko dla wybranej najlepszej daty
        hourScores[bestDate] = new Dictionary<int, int>();

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
                if (dateTime.Date != bestDate)
                    continue;

                int hour = dateTime.Hour;
                if (!hourScores[bestDate].ContainsKey(hour))
                    hourScores[bestDate][hour] = 0;

                // Dodaj podstawowe punkty
                hourScores[bestDate][hour] += points;
            }
        }

        // Znajdź najlepszą godzinę dla najlepszej daty
        int bestHour = 9;
        if (hourScores.ContainsKey(bestDate) && hourScores[bestDate].Any())
        {
            var bestHourEntry = hourScores[bestDate].OrderByDescending(kvp => kvp.Value).First();
            bestHour = bestHourEntry.Key;
        }

        DateTime bestTime = bestDate.Date.AddHours(bestHour);
        return (bestDate, bestTime);
    }
}