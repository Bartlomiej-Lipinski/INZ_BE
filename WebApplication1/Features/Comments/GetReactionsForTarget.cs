﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Comments;

public class GetReactionsForTarget: IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/reactions/{targetId}", Handle)
            .WithName("GetReactionsForTarget")
            .WithDescription("Retrieves reactions for a target")
            .WithTags("Reactions")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string targetId,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetReactionsForTarget> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

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

    public record ReactionDto
    {
        public string UserId { get; set; } = null!;
    }
}