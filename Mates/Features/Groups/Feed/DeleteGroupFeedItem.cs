using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Service;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Groups.Feed;

public class DeleteGroupFeedItem : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/feed/{feedItemId}", Handle)
            .WithName("DeleteGroupFeedItem")
            .WithDescription("Deletes a feed item from a group")
            .WithTags("GroupFeed")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string feedItemId,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteGroupFeedItem> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} deleting feed item {FeedItemId} in group {GroupId}. TraceId: {TraceId}",
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
            logger.LogWarning("User {UserId} attempted to delete feed item {FeedItemId} not created by them. TraceId: {TraceId}", 
                userId, feedItemId, traceId);
            return Results.Forbid();
        }

        if (feedItem.StoredFile != null)
        {
            await storage.DeleteFileAsync(feedItem.StoredFile.Url, cancellationToken);
            dbContext.StoredFiles.Remove(feedItem.StoredFile);
        }

        dbContext.GroupFeedItems.Remove(feedItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted feed item {FeedItemId} in group {GroupId}. TraceId: {TraceId}",
            userId, feedItemId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Feed item deleted successfully.", feedItemId, traceId));
    }
}