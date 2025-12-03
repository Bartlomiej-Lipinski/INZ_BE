using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Comments;

public class PostComment : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/comments/{targetId}", Handle)
            .WithName("PostComment")
            .WithDescription("Adds a comment to a target by a group member")
            .WithTags("Comments")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
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
        var userId = currentUser.GetUserId();

        logger.LogInformation(
            "User {UserId} attempts to post a comment on target {TargetId} in group {GroupId}. TraceId: {TraceId}",
            userId, targetId, groupId, traceId);

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            logger.LogWarning("User {UserId} attempted to post empty comment on target {TargetId}. TraceId: {TraceId}",
                userId, targetId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Comment content cannot be empty.", traceId));
        }

        if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
        {
            logger.LogWarning(
                "User {UserId} provided invalid entity type '{EntityType}' for target {TargetId}. TraceId: {TraceId}",
                userId, request.EntityType, targetId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Invalid entity type.", traceId));
        }

        object? target = entityType switch
        {
            EntityType.Recommendation => await dbContext.Recommendations
                .Include(r => r.Group)
                .FirstOrDefaultAsync(r => r.Id == targetId, cancellationToken),

            EntityType.Challenge => await dbContext.Challenges
                .Include(r => r.Group)
                .FirstOrDefaultAsync(r => r.Id == targetId, cancellationToken),
            EntityType.Comment => await dbContext.Comments
                .Include(c => c.Group)
                .FirstOrDefaultAsync(c => c.Id == targetId, cancellationToken),
            EntityType.GroupFeedItem => await dbContext.GroupFeedItems
                .Include(gfi => gfi.Group)
                .FirstOrDefaultAsync(gfi => gfi.Id == targetId, cancellationToken),

            _ => null
        };

        if (target == null)
        {
            logger.LogWarning("Target {TargetId} not found for entity type {EntityType}. TraceId: {TraceId}",
                targetId, entityType, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Target not found.", traceId));
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            TargetId = targetId,
            EntityType = entityType,
            UserId = userId!,
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