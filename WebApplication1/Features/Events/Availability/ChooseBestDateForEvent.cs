using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Events.Availability;

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

        var evt = await dbContext.Events
            .Include(e => e.Suggestions)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (evt == null)
        {
            logger.LogWarning("Event {EventId} not found. TraceId: {TraceId}", eventId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
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