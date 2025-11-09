using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Features.Recommendations.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

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
            .AddEndpointFilter<GroupMembershipFilter>();
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
        logger.LogInformation("Fetching recommendations for group {GroupId}. TraceId: {TraceId}", groupId, traceId);
        
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
        {
            logger.LogInformation(
                "[GetGroupRecommendations] No recommendations found for group {GroupId}. TraceId: {TraceId}", groupId,
                traceId);
            return Results.Ok(ApiResponse<List<RecommendationResponseDto>>
                .Ok([], "No recommendations found.", traceId));
        }

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
        
        if (response.Count == 0)
            return Results.Ok(ApiResponse<List<RecommendationResponseDto>>
                .Ok(response, "No recommendations found for this group.", traceId));

        logger.LogInformation("Retrieved {Count} recommendations for group {GroupId}. TraceId: {TraceId}",
            response.Count, groupId, traceId);
        return Results.Ok(ApiResponse<List<RecommendationResponseDto>>
            .Ok(response, "Group recommendations retrieved successfully.", traceId));
    }
}