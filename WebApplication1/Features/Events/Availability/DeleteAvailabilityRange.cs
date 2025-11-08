using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Events.Availability;

public class DeleteAvailabilityRange: IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/events/{eventId}/availability-range", Handle)
            .WithName("DeleteAvailabilityRange")
            .WithDescription("Deletes all availability ranges for the current user within an event")
            .WithTags("Availability")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteAvailabilityRange> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        var evt = await dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));

        var existingRanges = await dbContext.EventAvailabilityRanges
            .Where(ar => ar.EventId == eventId && ar.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existingRanges.Count == 0)
            return Results.Ok(ApiResponse<string>.Ok("No availability ranges to delete.", traceId));

        dbContext.EventAvailabilityRanges.RemoveRange(existingRanges);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted {Count} availability ranges for event {EventId}. TraceId: {TraceId}",
            userId, existingRanges.Count, eventId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Availability ranges deleted successfully.", traceId));
    }
}