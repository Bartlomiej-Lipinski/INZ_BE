using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Settlements;

public class GetUserSettlements :IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/settlements", Handle)
            .WithName("GetUserSettlements")
            .WithDescription("Retrieves all settlements for current user for a specific group")
            .WithTags("Settlements")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetUserSettlements> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to get settlements in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
        
        var settlements = await dbContext.Settlements
            .AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.FromUser)
            .Include(s => s.ToUser)
            .Where(s => s.GroupId == groupId && s.FromUserId == userId)
            .Select(s => new SettlementResponseDto
            {
                Id = s.Id,
                GroupId = s.GroupId,
                ToUserId = s.ToUserId,
                Amount = s.Amount,
                
            })
            .ToListAsync(cancellationToken);
        
        return Results.Ok(ApiResponse<List<SettlementResponseDto>>
            .Ok(settlements, "User settlements retrieved successfully.", traceId));
    }
}