using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class DeleteComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/comments/{targetId}/{commentId}", Handle)
            .WithName("DeleteComment")
            .WithDescription("Deletes a comment from a target. Allowed for comment author and target author.")
            .WithTags("Comments")
            .RequireAuthorization();
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
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;
        
        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }
        
        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to delete comment in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
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
                .AnyAsync(r => r.Id == targetId && r.UserId == userId, cancellationToken),

            _ => false
        };
        
        if (comment.UserId != userId && !isTargetOwner)
        {
            logger.LogWarning("User {UserId} attempted to delete comment {CommentId} without permission. TraceId: {TraceId}",
                userId, commentId, traceId);
            return Results.Forbid();
        }

        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted comment {CommentId} from target {TargetId}." +
                              " TraceId: {TraceId}", userId, commentId, targetId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Comment deleted successfully.", comment.Id, traceId));
    }
}