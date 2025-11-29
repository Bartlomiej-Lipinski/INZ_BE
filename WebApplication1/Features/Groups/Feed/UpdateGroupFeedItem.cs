using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Groups.Feed;

public class UpdateGroupFeedItem : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/feed/{feedItemId}", Handle)
            .WithName("UpdateGroupFeedItem")
            .WithDescription("Update an existing feed item in a group")
            .WithTags("GroupFeed")
            .RequireAuthorization()
            .Accepts<GroupFeedItemRequestDto>("multipart/form-data")
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string feedItemId,
        [FromForm] GroupFeedItemRequestDto request,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateGroupFeedItem> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} updating feed item {FeedItemId} in group {GroupId}. TraceId: {TraceId}",
            userId, feedItemId, groupId, traceId);
        
        var feedItem = await dbContext.GroupFeedItems
            .Include(f => f.StoredFile)
            .SingleOrDefaultAsync(f => f.Id == feedItemId && f.GroupId == groupId, cancellationToken);

        if (feedItem == null)
        {
            logger.LogWarning("Feed item not found. User {UserId}, FeedItemId {FeedItemId}, Group {GroupId}, TraceId: {TraceId}",
                userId, feedItemId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Feed item not found.", traceId));
        }

        if (feedItem.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to update feed item {FeedItemId} not created by them. TraceId: {TraceId}", 
                userId, feedItemId, traceId);
            return Results.Forbid();
        }
        
        if (string.IsNullOrWhiteSpace(request.Description) && request.File == null)
            return Results.BadRequest(ApiResponse<string>.Fail("Feed item must contain text or a file.", traceId));
        
        if (request.File != null)
        {
            string url;
            await using (var stream = request.File.OpenReadStream())
            {
                url = await storage.SaveFileAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    cancellationToken);
            }

            if (feedItem.StoredFile != null)
            {
                feedItem.StoredFile.FileName = request.File.FileName;
                feedItem.StoredFile.ContentType = request.File.ContentType;
                feedItem.StoredFile.Size = request.File.Length;
                feedItem.StoredFile.Url = url;
                feedItem.StoredFile.UploadedAt = DateTime.UtcNow;
            }
            else
            {
                var storedFileId = Guid.NewGuid().ToString();
                var storedFile = new StoredFile
                {
                    Id = storedFileId,
                    GroupId = groupId,
                    UploadedById = userId,
                    EntityType = EntityType.GroupFeedItem,
                    EntityId = feedItem.Id,
                    FileName = request.File.FileName,
                    ContentType = request.File.ContentType,
                    Size = request.File.Length,
                    Url = url,
                    UploadedAt = DateTime.UtcNow
                };
                feedItem.StoredFileId = storedFileId;
                dbContext.StoredFiles.Add(storedFile);
            }
        }

        feedItem.Description = request.Description;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated feed item {FeedItemId} in group {GroupId}. TraceId: {TraceId}",
            userId, feedItemId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Feed item updated successfully.", feedItem.Id, traceId));
    }
}