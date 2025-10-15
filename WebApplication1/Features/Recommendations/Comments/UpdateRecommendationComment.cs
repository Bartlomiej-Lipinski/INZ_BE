using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Recommendations.Comments;

public class UpdateRecommendationComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/recommendations/{recommendationId}/comments/{commentId}", Handle)
            .WithName("UpdateRecommendationComment")
            .WithDescription("Updates an existing comment for a recommendation")
            .WithTags("Recommendation Comments")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string recommendationId,
        [FromRoute] string commentId,
        [FromBody] UpdateCommentRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateRecommendationComment> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to update comment {CommentId}. TraceId: {TraceId}", 
                commentId, traceId);
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Content is required.", traceId));
        }

        var comment = await dbContext.RecommendationComments
            .Include(c => c.Recommendation)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.RecommendationId == recommendationId, cancellationToken);

        if (comment == null)
        {
            logger.LogWarning("Comment {CommentId} not found in recommendation {RecommendationId}. TraceId: {TraceId}",
                commentId, recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Comment not found.", traceId));
        }

        if (comment.UserId != currentUserId)
        {
            logger.LogWarning("User {UserId} tried to edit comment {CommentId} without permission. TraceId: {TraceId}",
                currentUserId, commentId, traceId);
            return Results.Forbid();
        }

        comment.Content = request.Content.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated comment {CommentId}. TraceId: {TraceId}",
            currentUserId, commentId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Comment updated successfully.", comment.Id, traceId));
    }

    public record UpdateCommentRequestDto
    {
        public string Content { get; set; } = null!;
    }
}