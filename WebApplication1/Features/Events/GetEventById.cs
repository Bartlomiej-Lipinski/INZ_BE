using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
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
            .RequireAuthorization()
            .WithOpenApi();
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
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to get event. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        if (group.GroupUsers.All(gu => gu.UserId != userId))
        {
            return Results.Forbid();
        }

        var evt = await dbContext.Events
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Group)
            .Include(e => e.Suggestions)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }
        
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