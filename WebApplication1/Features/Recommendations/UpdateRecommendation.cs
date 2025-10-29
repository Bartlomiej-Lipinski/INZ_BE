using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Recommendations.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Recommendations;

public class UpdateRecommendation : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/recommendations/{recommendationId}", Handle)
            .WithName("UpdateRecommendation")
            .WithDescription("Updates an existing recommendation if the user is the author.")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string recommendationId,
        [FromBody] RecommendationRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateRecommendation> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to update recommendation. TraceId: {TraceId}", traceId);
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
            logger.LogWarning("User {UserId} attempted to edit recommendation {RecommendationId} they do not own. " +
                              "TraceId: {TraceId}", currentUserId, recommendationId, traceId);
            return Results.Forbid();
        }
        
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
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
            currentUserId, recommendationId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Recommendation updated successfully.", 
            recommendationId, traceId));
    }
}