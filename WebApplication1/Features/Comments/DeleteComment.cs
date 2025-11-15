using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Comments;

public class DeleteComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/comments/{targetId}/{commentId}", Handle)
            .WithName("DeleteComment")
            .WithDescription("Deletes a comment from a target. Allowed for comment author and target author.")
            .WithTags("Comments")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string targetId,
        [FromRoute] string commentId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteComment> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        var comment = await dbContext.Comments
            .SingleOrDefaultAsync(c => c.Id == commentId && c.TargetId == targetId, cancellationToken);

        if (comment == null)
        {
            logger.LogWarning("Comment {CommentId} not found for target {TargetId}. TraceId: {TraceId}",
                commentId, targetId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Comment not found.", traceId));
        }

        var isTargetOwner = comment.EntityType switch
        {
            EntityType.Recommendation => await dbContext.Recommendations
                .AnyAsync(r => r.Id == targetId && r.UserId == userId, cancellationToken),

            _ => false
        };
        
        if (comment.UserId != userId && !isTargetOwner)
        {
            logger.LogWarning("User {UserId} attempted to delete comment {CommentId} without permission. TraceId: {TraceId}",
                userId, commentId, traceId);
            return Results.Forbid();
        }
        
        var relatedReactions = await dbContext.Reactions
            .Where(r => r.TargetId == commentId)
            .ToListAsync(cancellationToken);

        if (relatedReactions.Count > 0)
        {
            dbContext.Reactions.RemoveRange(relatedReactions);
            logger.LogInformation("Deleted {Count} reactions linked to comment {CommentId}. TraceId: {TraceId}",
                relatedReactions.Count, commentId, traceId);
        }
        
        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted comment {CommentId} from target {TargetId}. TraceId: {TraceId}",
            userId, commentId, targetId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Comment deleted successfully.", comment.Id, traceId));
    }
}