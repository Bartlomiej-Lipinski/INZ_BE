using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Recommendations;

public class DeleteRecommendation : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/recommendations/{recommendationId}", Handle)
            .WithName("DeleteRecommendation")
            .WithDescription("Deletes a recommendation and its related comments and reactions")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string recommendationId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteRecommendation> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to delete recommendation. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        var recommendation = await dbContext.Recommendations
            .FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {RecommendationId} not found. TraceId: {TraceId}", recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }
        
        if (recommendation.UserId != currentUserId)
        {
            logger.LogWarning("User {UserId} tried to delete recommendation {RecommendationId} not owned by them. " +
                              "TraceId: {TraceId}", currentUserId, recommendationId, traceId);
            return Results.Forbid();
        }
        
        var relatedComments = await dbContext.Comments
            .Where(c => c.TargetId == recommendationId)
            .ToListAsync(cancellationToken);

        var relatedReactions = await dbContext.Reactions
            .Where(r => r.TargetId == recommendationId)
            .ToListAsync(cancellationToken);
        
        if (relatedComments.Count > 0)
        {
            dbContext.Comments.RemoveRange(relatedComments);
            logger.LogInformation("Deleted {Count} comments linked to recommendation {RecommendationId}. TraceId: {TraceId}",
                relatedComments.Count, recommendationId, traceId);
        }

        if (relatedReactions.Count > 0)
        {
            dbContext.Reactions.RemoveRange(relatedReactions);
            logger.LogInformation("Deleted {Count} reactions linked to recommendation {RecommendationId}. TraceId: {TraceId}",
                relatedReactions.Count, recommendationId, traceId);
        }
        
        dbContext.Recommendations.Remove(recommendation);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Recommendation {RecommendationId} deleted by user {UserId}. TraceId: {TraceId}", 
            recommendationId, currentUserId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Recommendation deleted successfully.", recommendationId, traceId));
    }
}