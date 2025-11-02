using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events.Availability;

public class ChooseBestDateForEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{eventId}/{suggestionId}/choose-best-date", Handle)
            .WithName("ChoseBestDateForEvent")
            .WithDescription("Chooses the best date for an event based on calculated suggestions")
            .WithTags("Events")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string eventId,
        [FromRoute] string suggestionId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<ChooseBestDateForEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (currentUser?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(currentUserId))
        {
            logger.LogWarning("Unauthorized access or missing user id. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var evt = await dbContext.Events
            .Include(e => e.Suggestions)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (evt == null)
        {
            logger.LogWarning("Event not found. TraceId: {TraceId}", eventId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }

        var isUserInGroup = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == evt.GroupId && gu.UserId == currentUserId, cancellationToken);

        if (!isUserInGroup)
        {
            logger.LogWarning("User {UserId} is not a member of group {GroupId}. TraceId: {TraceId}",
                currentUserId, evt.GroupId, traceId);
            return Results.Forbid();
        }

        evt.StartDate = evt.Suggestions.FirstOrDefault(s => s.Id == suggestionId)?.StartTime;
        evt.IsAutoScheduled = false;
        if (evt.StartDate != null)
        {
            evt.Suggestions.Clear();
        }

        await dbContext.SaveChangesAsync(cancellationToken);


        return Results.Ok(ApiResponse<string>.Ok(null!, "Best date chosen successfully.", traceId));
    }
}