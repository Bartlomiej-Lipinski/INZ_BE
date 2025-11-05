using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Settlements;

public class DeleteSettlement : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/settlements/{settlementId}", Handle)
            .WithName("DeleteSettlement")
            .WithDescription("Deletes a specific settlement marking it as paid")
            .WithTags("Settlements")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string settlementId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteSettlement> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to delete settlement. TraceId: {TraceId}", traceId);
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
            logger.LogWarning("User {UserId} attempted to delete settlement in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
        
        var settlement = await dbContext.Settlements
            .FirstOrDefaultAsync(s => s.Id == settlementId && s.FromUserId == userId, cancellationToken);
        
        if (settlement == null)
        {
            logger.LogWarning("Settlement {Id} not found for user {UserId}. TraceId: {TraceId}", settlementId, userId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Settlement not found.", traceId));
        }
            
        dbContext.Settlements.Remove(settlement);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted settlement {Id} from group {GroupId}. TraceId: {TraceId}",
            userId, settlementId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Settlement deleted successfully.", settlementId, traceId));
    }
}