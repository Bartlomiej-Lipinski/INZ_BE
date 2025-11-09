using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Comments;

public class UpdateComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/comments/{targetId}/{commentId}", Handle)
            .WithName("UpdateComment")
            .WithDescription("Updates an existing comment for a target")
            .WithTags("Comments")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
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
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} attempts to update comment {CommentId} for target {TargetId}. TraceId: {TraceId}",
            userId, commentId, targetId, traceId);

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            logger.LogWarning("Empty content submitted for comment update by user {UserId}. TraceId: {TraceId}",
                userId, traceId);
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