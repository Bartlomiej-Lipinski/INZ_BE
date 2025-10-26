using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class PostComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/comments/{targetId}", Handle)
            .WithName("PostComment")
            .WithDescription("Adds a comment to a target by a group member")
            .WithTags("Comments")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string targetId,
        [FromBody] CommentRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostComment> logger,
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

        var target = request.TargetType switch
        {
            "Recommendation" => await dbContext.Recommendations
                .Include(r => r.Group)
                .FirstOrDefaultAsync(r => r.Id == targetId, cancellationToken),
            
            _ => null
        };
        
        if (target == null)
        {
            logger.LogWarning("Target {TargetId} not found. TraceId: {TraceId}", targetId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Target not found.", traceId));
        }

        var isMember = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == target.GroupId && gu.UserId == currentUserId, cancellationToken);

        if (!isMember)
        {
            logger.LogWarning("User {UserId} is not a member of group {GroupId}. TraceId: {TraceId}",
                currentUserId, target.GroupId, traceId);
            return Results.Forbid();
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid().ToString(),
            TargetId = targetId,
            TargetType = request.TargetType,
            UserId = currentUserId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} added comment {CommentId} to target {TargetId}. " +
                              "TraceId: {TraceId}", currentUserId, comment.Id, targetId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Comment added successfully.", comment.Id, traceId));
    }
}