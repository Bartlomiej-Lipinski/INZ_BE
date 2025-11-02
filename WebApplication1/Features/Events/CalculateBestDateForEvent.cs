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

        if (currentUser?.Identity?.IsAuthenticated != true) return TypedResults.Unauthorized();

        var evt = await dbContext.Events
            .Include(e => e.Suggestions)
            .Include(e => e.AvailabilityRanges)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

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

        if (evt.Suggestions.Count != 0)
        {
            evt.Suggestions.Clear();
        }

        var topDates = GetBestDateAndTime(evt);
        foreach (var (date, availablePeople) in topDates)
        {
            var evtSuggestion = new EventSuggestion
            {
                Id = Guid.NewGuid().ToString(),
                EventId = evt.Id,
                StartTime = date,
                AvailableUserCount = availablePeople
            };
            evt.Suggestions.Add(evtSuggestion);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ApiResponse<List<EventSuggestion>>.Ok(evt.Suggestions.ToList()));
    }

    // ... reszta klasy bez zmian ...
    public static List<(DateTime date, int availablePeople)> GetBestDateAndTime(Event ev)
    {
        Dictionary<DateTime, HashSet<string>> dateUsers = new();
        Dictionary<DateTime, Dictionary<int, HashSet<string>>> hourUsers = new();

        foreach (var availability in ev.AvailabilityRanges)
        {
            for (var date = availability.AvailableFrom.Date;
                 date <= availability.AvailableTo.Date;
                 date = date.AddDays(1))
            {
                if (date < ev.StartDate || date > ev.EndDate)
                    continue;

                if (!dateUsers.ContainsKey(date))
                    dateUsers[date] = new HashSet<string>();

                dateUsers[date].Add(availability.UserId);
            }
        }

        if (dateUsers.Count == 0)
        {
            var fallback = ev.StartDate ?? DateTime.UtcNow;
            return new List<(DateTime, int)> { (fallback.Date.AddHours(9), 0) };
        }

        var topDates = dateUsers
            .OrderByDescending(kvp => kvp.Value.Count)
            .Take(3)
            .ToList();

        var results = new List<(DateTime, int)>();

        foreach (var currentDate in topDates.Select(dateEntry => dateEntry.Key))
        {
            hourUsers[currentDate] = new Dictionary<int, HashSet<string>>();

            foreach (var availability in ev.AvailabilityRanges)
            {
                for (var dateTime = availability.AvailableFrom;
                     dateTime <= availability.AvailableTo;
                     dateTime = dateTime.AddHours(1))
                {
                    if (dateTime.Date != currentDate)
                        continue;

                    var hour = dateTime.Hour;
                    if (!hourUsers[currentDate].ContainsKey(hour))
                        hourUsers[currentDate][hour] = new HashSet<string>();

                    hourUsers[currentDate][hour].Add(availability.UserId);
                }
            }

            var bestHour = 9;
            var availablePeople = 0;

            if (hourUsers.TryGetValue(currentDate, out var value) && value.Count != 0)
            {
                var bestHourEntry = value.OrderByDescending(kvp => kvp.Value.Count).First();
                bestHour = bestHourEntry.Key;
                availablePeople = bestHourEntry.Value.Count;
            }

            var bestTime = currentDate.Date.AddHours(bestHour);
            results.Add((bestTime, availablePeople));
        }

        return results;
    }
}