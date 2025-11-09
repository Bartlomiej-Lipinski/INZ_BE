using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Comments;

public class GetCommentsForTarget : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/comments/{targetId}", Handle)
            .WithName("GetCommentsByTarget")
            .WithDescription("Retrieves comments for a target")
            .WithTags("Comments")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string targetId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetCommentsForTarget> logger,
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
                CreatedAt = c.CreatedAt.ToLocalTime()
            })
            .ToListAsync(cancellationToken);

        logger.LogInformation("Fetched {Count} comments for {TargetId}. TraceId: {TraceId}", 
            comments.Count, targetId, traceId);
        
        if (comments.Count == 0)
            return Results.Ok(ApiResponse<List<CommentResponseDto>>
                .Ok(comments, "No comments found for this target.", traceId));

        return Results.Ok(ApiResponse<List<CommentResponseDto>>
            .Ok(comments, "Comments retrieved successfully.", traceId));
    }
}