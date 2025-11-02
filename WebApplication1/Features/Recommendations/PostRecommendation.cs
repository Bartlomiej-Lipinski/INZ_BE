using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Recommendations.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

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
            .WithOpenApi();
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
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to post recommendation. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("GroupId, Title and Content are required.", 
                traceId));
        }

        var member = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == currentUserId, cancellationToken);

        if (member == null)
        {
            logger.LogWarning("User {UserId} is not a member of group {GroupId}. TraceId: {TraceId}",
                currentUserId, groupId, traceId);
            return Results.Forbid();
        }

        var recommendation = new Recommendation
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = currentUserId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Category = request.Category?.Trim(),
            ImageUrl = request.ImageUrl,
            LinkUrl = request.LinkUrl,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Recommendations.Add(recommendation);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} added new recommendation {RecommendationId} in group {GroupId}. TraceId: {TraceId}",
            currentUserId, recommendation.Id, groupId, traceId);

        return Results.Ok(ApiResponse<string>
            .Ok("Recommendation created successfully.", recommendation.Id, traceId));
    }
}