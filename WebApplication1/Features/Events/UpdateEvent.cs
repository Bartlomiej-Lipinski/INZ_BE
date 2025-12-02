using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Events;

public class UpdateEvent : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/events/{eventId}", Handle)
            .WithName("UpdateEvent")
            .WithDescription("Updates an existing event in a group")
            .WithTags("Events")
            .RequireAuthorization()
            .Accepts<EventRequestDto>("multipart/form-data")
            .AddEndpointFilter<GroupMembershipFilter>()
            .DisableAntiforgery();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string eventId,
        [FromForm] EventRequestDto request,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateEvent> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} started updating event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);
        
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingEvent = await dbContext.Events
            .SingleOrDefaultAsync(e => e.Id == eventId && e.GroupId == groupId, cancellationToken);

        if (existingEvent == null)
        {
            logger.LogWarning("Event not found. User {UserId}, EventId {EventId}, Group {GroupId}, TraceId: {TraceId}",
                userId, eventId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }

        if (existingEvent.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to update event {EventId} not created by them. TraceId: {TraceId}", 
                userId, eventId, traceId);
            return Results.Forbid();
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
            existingEvent.Title = request.Title;

        if (!string.IsNullOrWhiteSpace(request.Description))
            existingEvent.Description = request.Description;

        if (!string.IsNullOrWhiteSpace(request.Location))
            existingEvent.Location = request.Location;
        
        existingEvent.IsAutoScheduled = request.IsAutoScheduled;
        
        if (existingEvent.IsAutoScheduled)
        {
            if (!request.RangeStart.HasValue || !request.RangeEnd.HasValue || !request.DurationMinutes.HasValue)
            {
                logger.LogWarning("Missing parameters for auto-scheduled event. User {UserId}, EventId {EventId}, TraceId: {TraceId}",
                    userId, eventId, traceId);
                return Results.BadRequest(ApiResponse<string>.Fail(
                    "For automatic scheduling, range start, range end, and duration are required.", traceId));
            }
        }
        
        switch (request)
        {
            case { RangeStart: not null, RangeEnd: not null } when
                request.RangeEnd.Value < request.RangeStart.Value:
                logger.LogWarning("Invalid range dates. User {UserId}, EventId {EventId}, TraceId: {TraceId}",
                    userId, eventId, traceId);
                return Results.BadRequest(ApiResponse<string>
                    .Fail("Range end date cannot be earlier than start date.", traceId));
            
            case { StartDate: not null, EndDate: not null } when
                request.EndDate.Value < request.StartDate.Value:
                logger.LogWarning("Invalid start/end dates. User {UserId}, EventId {EventId}, TraceId: {TraceId}",
                    userId, eventId, traceId);
                return Results.BadRequest(ApiResponse<string>
                    .Fail("End date cannot be earlier than start date.", traceId));
        }

        existingEvent.RangeStart = request.RangeStart;
        existingEvent.RangeEnd = request.RangeEnd;
        existingEvent.DurationMinutes = request.DurationMinutes;
        existingEvent.StartDate = request.StartDate;
        existingEvent.EndDate = request.EndDate;
        
        var feedItem = await dbContext.GroupFeedItems
            .SingleOrDefaultAsync(f => f.EntityId == eventId && f.GroupId == groupId, cancellationToken);
        if (feedItem != null)
        {
            feedItem.Title = existingEvent.Title;
            feedItem.Description = existingEvent.Description;
        }

        if (request.File != null)
        {
            if (feedItem?.StoredFileId != null)
            {
                var oldFile = await dbContext.StoredFiles
                    .SingleOrDefaultAsync(f => f.Id == feedItem.StoredFileId, cancellationToken);

                if (oldFile != null)
                {
                    await storage.DeleteFileAsync(oldFile.Url, cancellationToken);
                    dbContext.StoredFiles.Remove(oldFile);
                }
            }
            
            string url;
            await using (var stream = request.File.OpenReadStream())
            {
                url = await storage.SaveFileAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    cancellationToken);
            }

            var storedFileId = Guid.NewGuid().ToString();

            var storedFile = new StoredFile
            {
                Id = storedFileId,
                GroupId = groupId,
                UploadedById = userId!,
                EntityType = EntityType.Event,
                EntityId = eventId,
                FileName = request.File.FileName,
                ContentType = request.File.ContentType,
                Size = request.File.Length,
                Url = url,
                UploadedAt = DateTime.UtcNow
            };
            
            feedItem!.StoredFileId = storedFileId;
            dbContext.StoredFiles.Add(storedFile);
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated event {EventId} in group {GroupId}. TraceId: {TraceId}",
            userId, eventId, groupId, traceId);

        var responseDto = new EventResponseDto
        {
            Id = existingEvent.Id,
            GroupId = existingEvent.GroupId,
            UserId = existingEvent.UserId,
            Title = existingEvent.Title,
            Description = existingEvent.Description,
            Location = existingEvent.Location,
            IsAutoScheduled = existingEvent.IsAutoScheduled,
            RangeStart = existingEvent.RangeStart?.ToLocalTime(),
            RangeEnd = existingEvent.RangeEnd?.ToLocalTime(),
            DurationMinutes = existingEvent.DurationMinutes,
            StartDate = existingEvent.StartDate?.ToLocalTime(),
            EndDate = existingEvent.EndDate?.ToLocalTime(),
            CreatedAt = existingEvent.CreatedAt.ToLocalTime()
        };

        return Results.Ok(ApiResponse<EventResponseDto>.Ok(responseDto, "Event updated successfully.", traceId));
    }
}