using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Events;

public class GetEventById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/events/{eventId}", Handle)
            .WithName("GetEventById")
            .WithDescription("Retrieves a single event by its ID within a group")
            .WithTags("Events")
            .RequireAuthorization();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetEventById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to retrieve event in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        var evt = await dbContext.Events
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Group)
            .Include(e => e.Suggestions)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        
        var availabilities = await dbContext.EventAvailabilities
            .AsNoTracking()
            .Include(c => c.User)
            .Where(ea => ea.EventId == eventId)
            .ToListAsync(cancellationToken);

        var response = new EventResponseDto
        {
            Id = evt.Id,
            GroupId = evt.GroupId,
            UserId = evt.UserId,
            Title = evt.Title,
            Description = evt.Description,
            Location = evt.Location,
            StartDate = evt.StartDate?.ToLocalTime(),
            EndDate = evt.EndDate?.ToLocalTime(),
            CreatedAt = evt.CreatedAt.ToLocalTime(),
            Availabilities = availabilities.Select(ea => new EventAvailabilityResponseDto
            {
                UserId = ea.UserId,
                Status = ea.Status,
                CreatedAt = ea.CreatedAt.ToLocalTime()
            }).ToList(),
            Suggestions = evt.Suggestions.Select(s => new EventSuggestionResponseDto
                {
                    StartTime = s.StartTime.ToLocalTime(),
                    AvailableUserCount = s.AvailableUserCount
                }).ToList() ?? []
        };

        return Results.Ok(ApiResponse<EventResponseDto>.Ok(response, null, traceId));
    }
}