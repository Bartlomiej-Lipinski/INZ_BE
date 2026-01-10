﻿using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Events.Dtos;
using Mates.Features.Storage.Dtos;
using Mates.Features.Users.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Events;

public class GetGroupEvents : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/events", Handle)
            .WithName("GetGroupEvents")
            .WithDescription("Retrieves all events for a specific group")
            .WithTags("Events")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupEvents> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} started fetching events for group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);

        var events = await dbContext.Events
            .AsNoTracking()
            .Include(e => e.Availabilities).ThenInclude(eventAvailability => eventAvailability.User)
            .Include(e => e.User)
            .Where(e => e.GroupId == groupId)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);

        var userIds = events
            .Select(p => p.UserId)
            .Concat(events.SelectMany(p => p.Availabilities).Select(ea => ea.UserId))
            .Distinct()
            .ToList();
        
        var profileFiles = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .ToListAsync(cancellationToken);

        var profilePictures = profileFiles
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionary(x => x.UploadedById);
        
        var eventsIds = events.Select(r => r.Id).ToList();
        
        var files = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f =>
                f.GroupId == groupId &&
                f.EntityType == EntityType.Event &&
                eventsIds.Contains(f.EntityId!))
            .ToListAsync(cancellationToken);

        var filesByEvent = files
            .GroupBy(f => f.EntityId)
            .ToDictionary(g => g.Key!, g => g.ToList());
        
        var response = events.Select(e => new EventResponseDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Location = e.Location,
                StartDate = e.StartDate?.ToLocalTime(),
                EndDate = e.EndDate?.ToLocalTime(),
                CreatedAt = e.CreatedAt.ToLocalTime(),
                IsAutoScheduled = e.IsAutoScheduled,
                User = new UserResponseDto
                {
                    Id = e.UserId,
                    Name = e.User.Name,
                    Surname = e.User.Surname,
                    Username = e.User.UserName,
                    ProfilePicture = profilePictures.TryGetValue(e.UserId, out var photo)
                        ? new ProfilePictureResponseDto
                        {
                            Url = photo.Url,
                            FileName = photo.FileName,
                            ContentType = photo.ContentType,
                            Size = photo.Size
                        }
                        : null
                },
                StoredFileId = filesByEvent.TryGetValue(e.Id, out var eventFiles) ? eventFiles.First().Id : null,
                Availabilities = e.Availabilities.Select(ea => new EventAvailabilityResponseDto
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
                                Id = availibilitiesPhoto.Id,
                                Url = availibilitiesPhoto.Url,
                                FileName = availibilitiesPhoto.FileName,
                                ContentType = availibilitiesPhoto.ContentType,
                                Size = availibilitiesPhoto.Size
                            }
                            : null
                    },
                    Status = ea.Status,
                    CreatedAt = ea.CreatedAt.ToLocalTime()
                }).ToList()
            })
            .ToList();

        if (events.Count == 0)
        {
            logger.LogInformation("No events found for group {GroupId}. TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<List<EventResponseDto>>
                .Ok(response, "No events found for this group.", traceId));
        }

        logger.LogInformation("User {UserId} retrieved {Count} events for group {GroupId}. TraceId: {TraceId}",
            userId, events.Count, groupId, traceId);
        return Results.Ok(ApiResponse<List<EventResponseDto>>.Ok(response, "Group events retrieved successfully.",
            traceId));
    }
}