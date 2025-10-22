using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Recommendations;

public class GetRecommendationById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/recommendations/{id}", Handle)
            .WithName("GetRecommendationById")
            .WithDescription("Retrieves a single recommendation by its ID")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string id,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetRecommendationById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? currentUser.FindFirst("sub")?.Value;
        
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to get recommendation. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        if (string.IsNullOrWhiteSpace(id))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Recommendation ID is required.", traceId));
        }
        
        var recommendation = await dbContext.Recommendations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Group)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation not found: {RecommendationId}. TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }
        
        var comments = await dbContext.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.TargetId == id && c.TargetType == "Recommendation")
            .ToListAsync(cancellationToken);

        var reactions = await dbContext.Reactions
            .AsNoTracking()
            .Where(r => r.TargetId == id && r.TargetType == "Recommendation")
            .ToListAsync(cancellationToken);

        var response = new RecommendationResponseDto
        {
            Id = recommendation.Id,
            Title = recommendation.Title,
            Content = recommendation.Content,
            Category = recommendation.Category,
            ImageUrl = recommendation.ImageUrl,
            LinkUrl = recommendation.LinkUrl,
            CreatedAt = recommendation.CreatedAt.ToLocalTime(),
            UserId = recommendation.UserId,
            Comments = comments.Select(c => new RecommendationCommentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToLocalTime()
            }).ToList(),
            Reactions = reactions.Select(r => new RecommendationReactionDto
            {
                UserId = r.UserId
            }).ToList()
        };

        return Results.Ok(ApiResponse<RecommendationResponseDto>.Ok(response, null, traceId));
    }

    public record RecommendationResponseDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
        public string? LinkUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; } = null!;
        public List<RecommendationCommentDto> Comments { get; set; } = [];
        public List<RecommendationReactionDto> Reactions { get; set; } = [];
    }

    public record RecommendationCommentDto
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public record RecommendationReactionDto
    {
        public string UserId { get; set; } = null!;
    }
}