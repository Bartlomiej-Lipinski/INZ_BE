using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Settlements;

public class DeleteSettlement : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/settlements/{settlementId}", Handle)
            .WithName("DeleteSettlement")
            .WithDescription("Deletes a specific settlement marking it as paid")
            .WithTags("Settlements")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
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
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} attempting to delete settlement {SettlementId} in group {GroupId}. TraceId: {TraceId}",
            userId, settlementId, groupId, traceId);
        
        var settlement = await dbContext.Settlements
            .SingleOrDefaultAsync(s => s.Id == settlementId && s.FromUserId == userId, cancellationToken);
        
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