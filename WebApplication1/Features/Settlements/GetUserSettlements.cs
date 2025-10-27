﻿using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
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
            .RequireAuthorization()
            .WithOpenApi();
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
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to get settlements. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));

        if (group.GroupUsers.All(gu => gu.UserId != userId))
            return Results.Forbid();
        
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