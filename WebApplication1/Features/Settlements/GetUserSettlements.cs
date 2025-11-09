using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Settlements;

public class GetUserSettlements :IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/settlements", Handle)
            .WithName("GetUserSettlements")
            .WithDescription("Retrieves all settlements for current user for a specific group")
            .WithTags("Settlements")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
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
        
        if (settlements.Count == 0)
            return Results.Ok(ApiResponse<List<SettlementResponseDto>>
                .Ok(settlements, "No settlements found for this user.", traceId));
        
        return Results.Ok(ApiResponse<List<SettlementResponseDto>>
            .Ok(settlements, "User settlements retrieved successfully.", traceId));
    }
}