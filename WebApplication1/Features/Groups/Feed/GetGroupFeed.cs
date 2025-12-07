using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Groups.Feed;

public class GetGroupFeed : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/feed", Handle)
            .WithName("GetGroupFeed")
            .WithDescription("Retrieves all feed items for a specific group")
            .WithTags("GroupFeed")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetGroupFeed> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (page <= 0) page = 1;
        if (pageSize is <= 0 or > 100) pageSize = 20;

        logger.LogInformation("Fetching feed for group {GroupId}, page {Page}, pageSize {PageSize}. TraceId: {TraceId}",
            groupId, page, pageSize, traceId);

        var feedItemsQuery = dbContext.GroupFeedItems
            .AsNoTracking()
            .Include(f => f.StoredFile)
            .Include(f => f.User)
            .Where(f => f.GroupId == groupId)
            .OrderByDescending(f => f.CreatedAt);

        var totalItems = await feedItemsQuery.CountAsync(cancellationToken);

        var feedItems = await feedItemsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var feedItemIds = feedItems.Select(r => r.Id).ToList();

        var comments = await dbContext.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => feedItemIds.Contains(c.TargetId))
            .ToListAsync(cancellationToken);

        var reactions = await dbContext.Reactions
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => feedItemIds.Contains(r.TargetId))
            .ToListAsync(cancellationToken);

        var commentsByFeedItem = comments
            .GroupBy(c => c.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var reactionsByFeedItem = reactions
            .GroupBy(r => r.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var userIds = feedItems.Select(f => f.UserId)
            .Concat(comments.Select(c => c.UserId))
            .Concat(reactions.Select(r => r.UserId))
            .Distinct()
            .ToList();

        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);

        var feedDtos = feedItems.Select(f => new GroupFeedItemResponseDto
        {
            Id = f.Id,
            Type = f.Type,
            Title = f.Title,
            Description = f.Description,
            CreatedAt = f.CreatedAt.ToLocalTime(),
            User = new UserResponseDto
            {
                Id = f.UserId,
                Name = f.User.Name,
                Surname = f.User.Surname,
                Username = f.User.UserName,
                ProfilePicture = profilePictures.TryGetValue(f.UserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Id = photo.Id,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null
            },
            StoredFileId = f.StoredFileId,
            EntityId = f.EntityId,
            Comments = commentsByFeedItem.TryGetValue(f.Id, out var itemComments)
                ? itemComments.Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    User = new UserResponseDto
                    {
                        Id = c.UserId,
                        Name = c.User.Name,
                        Surname = c.User.Surname,
                        Username = c.User.UserName,
                        ProfilePicture = profilePictures.TryGetValue(c.UserId, out var commentsPhoto)
                            ? new ProfilePictureResponseDto
                            {
                                Id = commentsPhoto.Id,
                                FileName = commentsPhoto.FileName,
                                ContentType = commentsPhoto.ContentType,
                                Size = commentsPhoto.Size
                            }
                            : null
                    },
                    Content = c.Content,
                    CreatedAt = c.CreatedAt.ToLocalTime()
                }).ToList()
                : [],
            Reactions = reactionsByFeedItem.TryGetValue(f.Id, out var itemReactions)
                ? itemReactions.Select(re => new UserResponseDto
                {
                    Id = re.UserId,
                    Name = re.User.Name,
                    Surname = re.User.Surname,
                    Username = re.User.UserName,
                    ProfilePicture = profilePictures.TryGetValue(re.UserId, out var reactionsPhoto)
                        ? new ProfilePictureResponseDto
                        {
                            Id = reactionsPhoto.Id,
                            FileName = reactionsPhoto.FileName,
                            ContentType = reactionsPhoto.ContentType,
                            Size = reactionsPhoto.Size
                        }
                        : null
                }).ToList()
                : []
        }).ToList();

        var response = new PagedApiResponse<GroupFeedItemResponseDto>
        {
            Data = feedDtos,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            Success = true,
            Message = feedDtos.Count == 0 ? "No feed items found for this group." : "Feed retrieved successfully",
            TraceId = traceId
        };

        logger.LogInformation("Retrieved {Count} feed items for group {GroupId}, traceId: {TraceId}",
            feedDtos.Count, groupId, traceId);
        return Results.Ok(response);
    }
}