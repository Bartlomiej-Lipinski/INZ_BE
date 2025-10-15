using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Recommendations;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Recommendations.Comments;

public class PostRecommendationComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/recommendations/{recommendationId}/comments", Handle)
            .WithName("PostRecommendationComment")
            .WithDescription("Adds a comment to a recommendation by a group member")
            .WithTags("Recommendation Comments")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string recommendationId,
        [FromBody] CommentRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostRecommendationComment> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to post a comment. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Comment content cannot be empty.", traceId));
        }

        var recommendation = await dbContext.Recommendations
            .Include(r => r.Group)
            .FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {RecommendationId} not found. TraceId: {TraceId}", recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }

        var isMember = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == recommendation.GroupId && gu.UserId == currentUserId, cancellationToken);

        if (!isMember)
        {
            logger.LogWarning("User {UserId} is not a member of group {GroupId}. TraceId: {TraceId}",
                currentUserId, recommendation.GroupId, traceId);
            return Results.Forbid();
        }

        var comment = new RecommendationComment
        {
            Id = Guid.NewGuid().ToString(),
            RecommendationId = recommendationId,
            UserId = currentUserId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.RecommendationComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} added comment {CommentId} to recommendation {RecommendationId}. " +
                              "TraceId: {TraceId}", currentUserId, comment.Id, recommendationId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Comment added successfully.", comment.Id, traceId));
    }

    public record CommentRequestDto
    {
        public string Content { get; set; } = null!;
    }
}