using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Comments;

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
        
        if (!Enum.TryParse<EntityType>(entityType, true, out var parsedEntityType))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Invalid entity type."));
        }

        object? target = parsedEntityType switch
        {
            EntityType.Recommendation => await dbContext.Recommendations
                .Include(r => r.Group)
                .FirstOrDefaultAsync(r => r.Id == targetId, cancellationToken),
            
            EntityType.Challenge => await dbContext.Challenges
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