using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Recommendations.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Recommendations;

public class UpdateRecommendation : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/recommendations/{recommendationId}", Handle)
            .WithName("UpdateRecommendation")
            .WithDescription("Updates an existing recommendation if the user is the author.")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string recommendationId,
        [FromBody] RecommendationRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateRecommendation> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} attempting to update recommendation {RecommendationId} in group {GroupId}. TraceId: {TraceId}", 
            userId, recommendationId, groupId, traceId);
        
        var recommendation = await dbContext.Recommendations
            .FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {RecommendationId} not found. TraceId: {TraceId}", recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }
        
        if (recommendation.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to edit recommendation {RecommendationId} they do not own. " +
                              "TraceId: {TraceId}", userId, recommendationId, traceId);
            return Results.Forbid();
        }
        
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            logger.LogWarning("Invalid update data from user {UserId}. TraceId: {TraceId}", userId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Title and Content are required.", traceId));
        }
        
        recommendation.Title = request.Title.Trim();
        recommendation.Content = request.Content.Trim();
        recommendation.Category = request.Category?.Trim();
        recommendation.ImageUrl = request.ImageUrl;
        recommendation.LinkUrl = request.LinkUrl;
        recommendation.UpdatedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} updated recommendation {RecommendationId}. TraceId: {TraceId}",
            userId, recommendationId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Recommendation updated successfully.", 
            recommendationId, traceId));
    }
}