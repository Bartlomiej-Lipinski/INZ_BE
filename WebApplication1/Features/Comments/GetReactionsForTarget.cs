using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class GetReactionsForTarget: IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/reactions/{targetId}", Handle)
            .WithName("GetReactionsForTarget")
            .WithDescription("Retrieves reactions for a target")
            .WithTags("Reactions")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string targetId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetReactionsForTarget> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to get reactions. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to get reactions in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        var reactions = await dbContext.Reactions
            .AsNoTracking()
            .Where(c => c.TargetId == targetId)
            .Select(c => new ReactionDto
            {
                UserId = c.UserId,
            })
            .ToListAsync(cancellationToken);

        if (reactions.Count == 0)
        {
            logger.LogInformation("No reactions found for {TargetId}. TraceId: {TraceId}", targetId, traceId);
            return Results.Ok(ApiResponse<List<ReactionDto>>
                .Ok([], "No reactions found.", traceId));
        }
        
        logger.LogInformation("Fetched {Count} reactions for {TargetId}. TraceId: {TraceId}", 
            reactions.Count, targetId, traceId);

        return Results.Ok(ApiResponse<List<ReactionDto>>
            .Ok(reactions, "Reactions retrieved successfully.", traceId));
    }
}