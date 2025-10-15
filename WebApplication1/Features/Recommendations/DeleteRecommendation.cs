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
        app.MapDelete("/recommendations/{id}", Handle)
            .WithName("DeleteRecommendation")
            .WithDescription("Deletes a recommendation and its related comments and reactions")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string id,
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
            .Include(r => r.Comments)
            .Include(r => r.Reactions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {RecommendationId} not found. TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }
        
        if (recommendation.UserId != currentUserId)
        {
            logger.LogWarning("User {UserId} tried to delete recommendation {RecommendationId} not owned by them. " +
                              "TraceId: {TraceId}", currentUserId, id, traceId);
            return Results.Forbid();
        }
        
        var comments = dbContext.RecommendationComments.Where(c => c.RecommendationId == id);
        var reactions = dbContext.RecommendationReactions.Where(r => r.RecommendationId == id);

        dbContext.RecommendationComments.RemoveRange(comments);
        dbContext.RecommendationReactions.RemoveRange(reactions);
        dbContext.Recommendations.Remove(recommendation);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Recommendation {RecommendationId} deleted by user {UserId}. TraceId: {TraceId}", 
            id, currentUserId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Recommendation deleted successfully.", id, traceId));
    }
}