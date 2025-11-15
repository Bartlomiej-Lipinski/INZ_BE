using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

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
            .AddEndpointFilter<GroupMembershipFilter>();
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
        var userId = currentUser.GetUserId();

        logger.LogInformation(
            "User {UserId} fetching reactions for target {TargetId} in group {GroupId}. TraceId: {TraceId}",
            userId, targetId, groupId, traceId);

        var reactions = await dbContext.Reactions
            .AsNoTracking()
            .Where(c => c.TargetId == targetId)
            .Select(c => new ReactionDto
            {
                UserId = c.UserId,
            })
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Fetched {Count} reactions for {TargetId}. TraceId: {TraceId}", 
            reactions.Count, targetId, traceId);
        
        if (reactions.Count == 0)
        {
            logger.LogInformation(
                "No reactions found for target {TargetId}. TraceId: {TraceId}", targetId, traceId);
            return Results.Ok(ApiResponse<List<ReactionDto>>
                .Ok(reactions, "No reactions found for this target.", traceId));
        }

        logger.LogInformation(
            "Retrieved {Count} reactions for target {TargetId}. TraceId: {TraceId}", 
            reactions.Count, targetId, traceId);
        return Results.Ok(ApiResponse<List<ReactionDto>>
            .Ok(reactions, "Reactions retrieved successfully.", traceId));
    }
}