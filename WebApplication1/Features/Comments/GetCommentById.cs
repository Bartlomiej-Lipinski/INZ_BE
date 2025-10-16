using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class GetCommentById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/recommendations/{recommendationId}/comments", Handle)
            .WithName("GetRecommendationComments")
            .WithDescription("Retrieves comments for a recommendation")
            .WithTags("Recommendation Comments")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string recommendationId,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetCommentById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var recommendation = await dbContext.Recommendations
            .AsNoTracking()
            .Include(r => r.Comments)
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {RecommendationId} not found. TraceId: {TraceId}", recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }

        var commentsDto = recommendation.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponseDto
            {
                Id = c.Id,
                Content = c.Content,
                UserId = c.UserId,
                UserName = c.User.UserName,
                CreatedAt = c.CreatedAt
            })
            .ToList();

        return Results.Ok(ApiResponse<List<CommentResponseDto>>
            .Ok(commentsDto, "Comments retrieved successfully", traceId));
    }

    public record CommentResponseDto
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string? UserName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}