using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class UpdateComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/comments/{targetId}/{commentId}", Handle)
            .WithName("UpdateComment")
            .WithDescription("Updates an existing comment for a target")
            .WithTags("Comments")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string targetId,
        [FromRoute] string commentId,
        [FromBody] CommentRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateComment> logger,
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
            logger.LogWarning("User {UserId} attempted to update comment in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Content is required.", traceId));
        }

        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TargetId == targetId, cancellationToken);

        if (comment == null)
        {
            logger.LogWarning("Comment {CommentId} not found in target {TargetId}. TraceId: {TraceId}",
                commentId, targetId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Comment not found.", traceId));
        }

        if (comment.UserId != userId)
        {
            logger.LogWarning("User {UserId} tried to edit comment {CommentId} without permission. TraceId: {TraceId}",
                userId, commentId, traceId);
            return Results.Forbid();
        }

        comment.Content = request.Content.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated comment {CommentId}. TraceId: {TraceId}",
            userId, commentId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Comment updated successfully.", comment.Id, traceId));
    }
}