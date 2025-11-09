using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Recommendations.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Recommendations;

public class PostRecommendation : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/recommendations", Handle)
            .WithName("PostRecommendation")
            .WithDescription("Creates a new recommendation within a group by a member")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] RecommendationRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostRecommendation> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Creating recommendation in group {GroupId} by user {UserId}. TraceId: {TraceId}", 
            groupId, userId, traceId);
        
        if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Content))
        {
            logger.LogWarning("Invalid recommendation data provided by user {UserId}. TraceId: {TraceId}",
                userId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("GroupId, Title and Content are required.", traceId));
        }

        var recommendation = new Recommendation
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId!,
            EntityType = EntityType.Recommendation,
            Title = request.Title,
            Content = request.Content,
            Category = request.Category,
            ImageUrl = request.ImageUrl,
            LinkUrl = request.LinkUrl,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Recommendations.Add(recommendation);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} added new recommendation {RecommendationId} in group {GroupId}. TraceId: {TraceId}",
            userId, recommendation.Id, groupId, traceId);
        return Results.Ok(ApiResponse<string>
            .Ok("Recommendation created successfully.", recommendation.Id, traceId));
    }
}