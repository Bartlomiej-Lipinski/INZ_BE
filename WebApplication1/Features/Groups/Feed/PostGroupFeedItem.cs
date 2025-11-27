using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Groups.Feed;

public class PostGroupFeedItem : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/feed", Handle)
            .WithName("PostGroupFeedItem")
            .WithDescription("Add a new feed item to a group")
            .WithTags("GroupFeed")
            .RequireAuthorization()
            .Accepts<GroupFeedItemRequestDto>("multipart/form-data")
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromForm] GroupFeedItemRequestDto request,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostGroupFeedItem> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} creating feed item in group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);
        
        if (string.IsNullOrWhiteSpace(request.Description) && request.File == null)
            return Results.BadRequest(ApiResponse<string>.Fail("Feed item must contain text or a file.", traceId));
        
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var feedItem = new GroupFeedItem
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            Type = FeedItemType.Post,
            Description = request.Description,
            UserId = userId!,
            CreatedAt = DateTime.UtcNow
        };
        
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

            var storedFileId = Guid.NewGuid().ToString();
            var storedFile = new StoredFile
            {
                Id = storedFileId,
                GroupId = groupId,
                UploadedById = userId!,
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

        dbContext.GroupFeedItems.Add(feedItem);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} added new feed item {ItemId} in group {GroupId}. TraceId: {TraceId}",
            userId, feedItem.Id, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Feed item created successfully.", feedItem.Id, traceId));
    }
}