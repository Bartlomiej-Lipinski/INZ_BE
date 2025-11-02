using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Features.Recommendations.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Recommendations;

public class GetGroupRecommendations: IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/recommendations", Handle)
            .WithName("GetGroupRecommendations")
            .WithDescription("Retrieves all recommendations for a specific group")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupRecommendations> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to get group events. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));

        var isMember = group.GroupUsers.Any(gu => gu.UserId == userId);
        if (!isMember)
        {
            logger.LogWarning("User {UserId} tried to get events for group {GroupId} without membership. TraceId: {TraceId}", 
                userId, groupId, traceId);
            return Results.Forbid();
        }
        
        var recommendations = await dbContext.Recommendations
            .AsNoTracking()
            .Where(r => r.GroupId == groupId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Content,
                r.Category,
                r.ImageUrl,
                r.LinkUrl,
                r.CreatedAt,
                r.UserId
            })
            .ToListAsync(cancellationToken);

        if (recommendations.Count == 0)
            return Results.Ok(ApiResponse<List<RecommendationResponseDto>>
                .Ok([], "No recommendations found.", traceId));

        var recommendationIds = recommendations.Select(r => r.Id).ToList();
        
        var comments = await dbContext.Comments
            .AsNoTracking()
            .Where(c => recommendationIds.Contains(c.TargetId))
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
            .Where(r => recommendationIds.Contains(r.TargetId))
            .Select(r => new
            {
                r.TargetId,
                r.UserId,
            })
            .ToListAsync(cancellationToken);

        var commentsByRecommendation = comments
            .GroupBy(c => c.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var reactionsByRecommendation = reactions
            .GroupBy(r => r.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var response = recommendations.Select(r => new RecommendationResponseDto
        {
            Id = r.Id,
            Title = r.Title,
            Content = r.Content,
            Category = r.Category,
            ImageUrl = r.ImageUrl,
            LinkUrl = r.LinkUrl,
            CreatedAt = r.CreatedAt.ToLocalTime(),
            UserId = r.UserId,
            Comments = commentsByRecommendation.TryGetValue(r.Id, out var recComments)
                ? recComments.Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt.ToLocalTime()
                }).ToList()
                : [],
            Reactions = reactionsByRecommendation.TryGetValue(r.Id, out var recReactions)
                ? recReactions.Select(re => new ReactionDto
                {
                    UserId = re.UserId,
                }).ToList()
                : []
        }).ToList();

        return Results.Ok(ApiResponse<List<RecommendationResponseDto>>
            .Ok(response, "Group recommendations retrieved successfully.", traceId));
    }
}