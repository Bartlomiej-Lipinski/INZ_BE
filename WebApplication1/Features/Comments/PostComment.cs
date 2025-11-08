using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class PostComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/comments/{targetId}", Handle)
            .WithName("PostComment")
            .WithDescription("Adds a comment to a target by a group member")
            .WithTags("Comments")
            .RequireAuthorization();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string targetId,
        [FromBody] CommentRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostComment> logger,
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
            logger.LogWarning("User {UserId} attempted to post a comment in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
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

        var comment = new Comment
        {
            Id = Guid.NewGuid().ToString(),
            TargetId = targetId,
            TargetType = request.TargetType,
            UserId = userId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} added comment {CommentId} to target {TargetId}. " +
                              "TraceId: {TraceId}", userId, comment.Id, targetId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Comment added successfully.", comment.Id, traceId));
    }
}