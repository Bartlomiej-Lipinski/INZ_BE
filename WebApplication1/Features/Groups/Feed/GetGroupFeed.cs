using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
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
            .Where(f => f.GroupId == groupId)
            .OrderByDescending(f => f.CreatedAt);

        var totalItems = await dbContext.GroupFeedItems
            .AsNoTracking()
            .Where(f => f.GroupId == groupId)
            .CountAsync(cancellationToken);
        
        var feedItems = await feedItemsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        var feedItemsIds = feedItems.Select(r => r.Id).ToList();
        
        var comments = await dbContext.Comments
            .AsNoTracking()
            .Where(c => feedItemsIds.Contains(c.TargetId))
            .Select(c => new
            {
                c.Id,
                c.TargetId,
                c.UserId,
                c.Content,
                c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var reactions = await dbContext.Reactions
            .AsNoTracking()
            .Where(r => feedItemsIds.Contains(r.TargetId))
            .Select(r => new
            {
                r.TargetId,
                r.UserId,
            })
            .ToListAsync(cancellationToken);
        
        var commentsByFeedItem = comments
            .GroupBy(c => c.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var reactionsByFeedItem = reactions
            .GroupBy(r => r.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        var feedDtos = feedItems.Select(f => new GroupFeedItemResponseDto
        {
            Id = f.Id,
            Type = f.Type,
            Title = f.Title,
            Description = f.Description,
            CreatedAt = f.CreatedAt.ToLocalTime(),
            UserId = f.UserId,
            StoredFileId = f.StoredFileId,
            EntityId = f.EntityId,
            Comments = commentsByFeedItem.TryGetValue(f.Id, out var itemComments)
                ? itemComments.Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt.ToLocalTime()
                }).ToList()
                : [],
            Reactions = reactionsByFeedItem.TryGetValue(f.Id, out var itemReactions)
                ? itemReactions.Select(re => new ReactionDto
                {
                    UserId = re.UserId,
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
