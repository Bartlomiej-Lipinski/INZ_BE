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

public class GetRecommendationById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/recommendations/{recommendationId}", Handle)
            .WithName("GetRecommendationById")
            .WithDescription("Retrieves a single recommendation by its ID")
            .WithTags("Recommendations")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string recommendationId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetRecommendationById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? currentUser.FindFirst("sub")?.Value;
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to get recommendation. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to get recommendation in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
        
        if (string.IsNullOrWhiteSpace(recommendationId))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Recommendation ID is required.", traceId));
        }
        
        var recommendation = await dbContext.Recommendations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Group)
            .FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation not found: {RecommendationId}. TraceId: {TraceId}", recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }
        
        var comments = await dbContext.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.TargetId == recommendationId && c.TargetType == "Recommendation")
            .ToListAsync(cancellationToken);

        var reactions = await dbContext.Reactions
            .AsNoTracking()
            .Where(r => r.TargetId == recommendationId && r.TargetType == "Recommendation")
            .ToListAsync(cancellationToken);

        var response = new RecommendationResponseDto
        {
            Id = recommendation.Id,
            Title = recommendation.Title,
            Content = recommendation.Content,
            Category = recommendation.Category,
            ImageUrl = recommendation.ImageUrl,
            LinkUrl = recommendation.LinkUrl,
            CreatedAt = recommendation.CreatedAt.ToLocalTime(),
            UserId = recommendation.UserId,
            Comments = comments.Select(c => new CommentResponseDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToLocalTime()
            }).ToList(),
            Reactions = reactions.Select(r => new ReactionDto
            {
                UserId = r.UserId
            }).ToList()
        };

        return Results.Ok(ApiResponse<RecommendationResponseDto>.Ok(response, null, traceId));
    }
}