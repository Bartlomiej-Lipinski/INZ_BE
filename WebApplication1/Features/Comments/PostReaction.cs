using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class PostReaction : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/reactions/{targetId}/{targetType}", Handle)
            .WithName("PostReaction")
            .WithDescription("Adds or removes a like reaction to a target by a group member")
            .WithTags("Reactions")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string targetId,
        [FromRoute] string targetType,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostReaction> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to react to target. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to add or remove a reaction in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        var target = targetType switch
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
            TargetId = targetId,
            TargetType = targetType,
            UserId = userId,
        };

        dbContext.Reactions.Add(reaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} added reaction to target {TargetId}. TraceId: {TraceId}",
            userId, targetId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Reaction added.", traceId));
    }
}