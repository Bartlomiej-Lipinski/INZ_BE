using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Recommendations.Comments;

public class DeleteRecommendationComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/recommendations/{recommendationId}/comments/{commentId}", Handle)
            .WithName("DeleteRecommendationComment")
            .WithDescription("Deletes a comment from a recommendation. " +
                             "Allowed for comment author or recommendation author.")
            .WithTags("Recommendation Comments")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string recommendationId,
        [FromRoute] string commentId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteRecommendationComment> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to delete comment {CommentId}. TraceId: {TraceId}", commentId, traceId);
            return Results.Unauthorized();
        }

        var comment = await dbContext.RecommendationComments
            .Include(c => c.Recommendation)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.RecommendationId == recommendationId, cancellationToken);

        if (comment == null)
        {
            logger.LogWarning("Comment {CommentId} not found for recommendation {RecommendationId}. TraceId: {TraceId}",
                commentId, recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Comment not found.", traceId));
        }

        if (comment.UserId != currentUserId && comment.Recommendation.UserId != currentUserId)
        {
            logger.LogWarning("User {UserId} attempted to delete comment {CommentId} without permission. TraceId: {TraceId}",
                currentUserId, commentId, traceId);
            return Results.Forbid();
        }

        dbContext.RecommendationComments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted comment {CommentId} from recommendation {RecommendationId}." +
                              " TraceId: {TraceId}", currentUserId, commentId, recommendationId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Comment deleted successfully.", comment.Id, traceId));
    }
}