using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class DeleteComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/comments/{targetId}/{commentId}", Handle)
            .WithName("DeleteComment")
            .WithDescription("Deletes a comment from a target. Allowed for comment author and target author.")
            .WithTags("Comments")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string targetId,
        [FromRoute] string commentId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteComment> logger,
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

        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TargetId == targetId, cancellationToken);

        if (comment == null)
        {
            logger.LogWarning("Comment {CommentId} not found for target {TargetId}. TraceId: {TraceId}",
                commentId, targetId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Comment not found.", traceId));
        }

        var isTargetOwner = comment.TargetType switch
        {
            "Recommendation" => await dbContext.Recommendations
                .AnyAsync(r => r.Id == targetId && r.UserId == currentUserId, cancellationToken),

            _ => false
        };
        
        if (comment.UserId != currentUserId && !isTargetOwner)
        {
            logger.LogWarning("User {UserId} attempted to delete comment {CommentId} without permission. TraceId: {TraceId}",
                currentUserId, commentId, traceId);
            return Results.Forbid();
        }

        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted comment {CommentId} from target {TargetId}." +
                              " TraceId: {TraceId}", currentUserId, commentId, targetId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Comment deleted successfully.", comment.Id, traceId));
    }
}