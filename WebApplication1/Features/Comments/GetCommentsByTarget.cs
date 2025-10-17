using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class GetCommentsByTarget : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/comments/{targetId}", Handle)
            .WithName("GetCommentsByTarget")
            .WithDescription("Retrieves comments for a target")
            .WithTags("Comments")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string targetId,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetCommentsByTarget> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var comments = await dbContext.Comments
            .AsNoTracking()
            .Where(c => c.TargetId == targetId)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentResponseDto
            {
                Id = c.Id,
                Content = c.Content,
                UserId = c.UserId,
                UserName = c.User.UserName,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        logger.LogInformation("Fetched {Count} comments for {TargetId}. TraceId: {TraceId}", 
            comments.Count, targetId, traceId);

        return Results.Ok(ApiResponse<List<CommentResponseDto>>
            .Ok(comments, "Comments retrieved successfully.", traceId));
    }

    public record CommentResponseDto
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string? UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}