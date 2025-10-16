using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Infrastructure.Data.Entities.Recommendations;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class PostReaction : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/recommendations/{recommendationId}/reactions", Handle)
            .WithName("PostRecommendationReaction")
            .WithDescription("Adds or removes a like reaction to a recommendation by a group member")
            .WithTags("Recommendation Reactions")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string recommendationId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostReaction> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to react to recommendation. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var recommendation = await dbContext.Recommendations
            .Include(r => r.Group)
            .FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {RecommendationId} not found. TraceId: {TraceId}", recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }

        var isMember = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == recommendation.GroupId && gu.UserId == currentUserId, cancellationToken);

        if (!isMember)
        {
            logger.LogWarning("User {UserId} is not a member of group {GroupId}. TraceId: {TraceId}",
                currentUserId, recommendation.GroupId, traceId);
            return Results.Forbid();
        }

        var existingReaction = await dbContext.Reactions
            .FirstOrDefaultAsync(rr => rr.TargetId == recommendationId 
                                       && rr.UserId == currentUserId, cancellationToken);

        if (existingReaction != null)
        {
            dbContext.Reactions.Remove(existingReaction);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} removed reaction from recommendation {RecommendationId}. TraceId: {TraceId}",
                currentUserId, recommendationId, traceId);

            return Results.Ok(ApiResponse<string>.Ok("Reaction removed.", traceId));
        }

        var reaction = new Reaction
        {
            TargetId = recommendationId,
            UserId = currentUserId,
        };

        dbContext.Reactions.Add(reaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} added reaction to recommendation {RecommendationId}. TraceId: {TraceId}",
            currentUserId, recommendationId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Reaction added.", traceId));
    }
}