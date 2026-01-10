using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Comments;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Comments;

public class PostReaction : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/reactions/{targetId}/{entityType}", Handle)
            .WithName("PostReaction")
            .WithDescription("Adds or removes a like reaction to a target by a group member")
            .WithTags("Reactions")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string targetId,
        [FromRoute] string entityType,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostReaction> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation(
            "User {UserId} attempts to toggle reaction on target {TargetId} in group {GroupId}. TraceId: {TraceId}",
            userId, targetId, groupId, traceId);

        if (!Enum.TryParse<EntityType>(entityType, true, out var parsedEntityType))
        {
            logger.LogWarning(
                "User {UserId} provided invalid entity type '{EntityType}' for target {TargetId}. TraceId: {TraceId}",
                userId, entityType, targetId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Invalid entity type.", traceId));
        }

        object? target = parsedEntityType switch
        {
            EntityType.Comment => await dbContext.Comments
                .Include(c => c.Group)
                .FirstOrDefaultAsync(r => r.Id == targetId, cancellationToken),

            EntityType.Recommendation => await dbContext.Recommendations
                .Include(r => r.Group)
                .FirstOrDefaultAsync(r => r.Id == targetId, cancellationToken),
            EntityType.GroupFeedItem => await dbContext.GroupFeedItems
                .Include(gfi => gfi.Group)
                .FirstOrDefaultAsync(r => r.Id == targetId, cancellationToken),
            _ => null
        };

        if (target == null)
        {
            logger.LogWarning("Target {TargetId} not found for entity type {EntityType}. TraceId: {TraceId}",
                targetId, parsedEntityType, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Target not found.", traceId));
        }

        var existingReaction = await dbContext.Reactions
            .FirstOrDefaultAsync(rr => rr.TargetId == targetId
                                       && rr.UserId == userId, cancellationToken);

        if (existingReaction != null)
        {
            dbContext.Reactions.Remove(existingReaction);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} removed reaction from target {TargetId}. TraceId: {TraceId}",
                userId, targetId, traceId);

            return Results.Ok(ApiResponse<string>.Ok("Reaction removed.", traceId));
        }

        var reaction = new Reaction
        {
            GroupId = groupId,
            TargetId = targetId,
            EntityType = parsedEntityType,
            UserId = userId!
        };

        dbContext.Reactions.Add(reaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} added reaction to target {TargetId}. TraceId: {TraceId}",
            userId, targetId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Reaction added.", traceId));
    }
}