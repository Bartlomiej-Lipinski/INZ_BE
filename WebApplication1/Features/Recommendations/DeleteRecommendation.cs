using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Recommendations;

public class DeleteRecommendation : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/recommendations/{recommendationId}", Handle)
            .WithName("DeleteRecommendation")
            .WithDescription("Deletes a recommendation and its related comments and reactions")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string recommendationId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteRecommendation> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} attempting to delete recommendation {RecommendationId} in group {GroupId}. TraceId: {TraceId}",
            userId, recommendationId, groupId, traceId);
        
        var recommendation = await dbContext.Recommendations
            .SingleOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {RecommendationId} not found. TraceId: {TraceId}", recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }
        
        var groupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = groupUser?.IsAdmin ?? false;
        if (recommendation.UserId != userId && !isAdmin)
        {
            logger.LogWarning("User {UserId} attempted to delete recommendation {Id} they do not own and is not admin. " +
                              "TraceId: {TraceId}", userId, recommendationId, traceId);
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
            recommendationId, userId, traceId);
        return Results.Ok(ApiResponse<string>
            .Ok("Recommendation deleted successfully.", recommendationId, traceId));
    }
}