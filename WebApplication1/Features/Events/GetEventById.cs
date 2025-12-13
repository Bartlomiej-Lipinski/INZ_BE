using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

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
            .AddEndpointFilter<GroupMembershipFilter>();
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

        logger.LogInformation("User {UserId} started fetching event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);


        var evt = await dbContext.Events
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Group)
            .Include(e => e.Suggestions)
            .Include(e => e.Availabilities)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (evt == null)
        {
            logger.LogWarning("Event {EventId} not found in group {GroupId}. TraceId: {TraceId}", eventId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }
        
        var userIds = evt.Availabilities.Select(a => a.UserId)
            .Append(evt.UserId).Distinct().ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);
        
        var response = new EventResponseDto
        {
            Id = evt.Id,
            GroupId = evt.GroupId,
            User = new UserResponseDto
            {
                Id = evt.UserId,
                Name = evt.User.Name,
                Surname = evt.User.Surname,
                Username = evt.User.UserName,
                ProfilePicture = profilePictures.TryGetValue(evt.UserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null 
            },
            Title = evt.Title,
            Description = evt.Description,
            Location = evt.Location,
            IsAutoScheduled = evt.IsAutoScheduled,
            StartDate = evt.StartDate?.ToLocalTime(),
            EndDate = evt.EndDate?.ToLocalTime(),
            CreatedAt = evt.CreatedAt.ToLocalTime(),
            Availabilities = evt.Availabilities.Select(ea => new EventAvailabilityResponseDto
            {
                User = new UserResponseDto
                {
                    Id = ea.UserId,
                    Name = ea.User.Name,
                    Surname = ea.User.Surname,
                    Username = ea.User.UserName,
                    ProfilePicture = profilePictures.TryGetValue(ea.UserId, out var availibilitiesPhoto)
                        ? new ProfilePictureResponseDto
                        {
                            Url = availibilitiesPhoto.Url,
                            FileName = availibilitiesPhoto.FileName,
                            ContentType = availibilitiesPhoto.ContentType,
                            Size = availibilitiesPhoto.Size
                        }
                        : null 
                },
                Status = ea.Status,
                CreatedAt = ea.CreatedAt.ToLocalTime()
            }).ToList(),
            Suggestions = evt.Suggestions.Select(s => new EventSuggestionResponseDto
            {
                StartTime = s.StartTime.ToLocalTime(),
                AvailableUserCount = s.AvailableUserCount
            }).ToList()
        };

        logger.LogInformation("User {UserId} successfully fetched event {EventId} from group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);
        return Results.Ok(ApiResponse<EventResponseDto>.Ok(response, null, traceId));
    }
}