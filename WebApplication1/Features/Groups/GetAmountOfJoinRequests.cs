using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GetAmountOfJoinRequests : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/join-requests/amount/{userId}", Handle)
            .WithName("GetAmountOfJoinRequests")
            .WithDescription("Returns the amount of join requests for the current user")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetAmountOfJoinRequests> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("User ID not found in claims. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("User ID cannot be null or empty.", traceId));
        }

        var adminGroupIds = await dbContext.GroupUsers
            .Where(gu => gu.UserId == userId && gu.IsAdmin)
            .Select(gu => gu.GroupId)
            .ToListAsync(cancellationToken);
        
        var amount = await dbContext.GroupUsers
            .AsNoTracking()
            .Where(gu => adminGroupIds.Contains(gu.GroupId) && gu.AcceptanceStatus == AcceptanceStatus.Pending && !gu.IsAdmin)
            .CountAsync(cancellationToken);
        
        logger.LogInformation("Pending join requests fetched for user {UserId}. Amount: {Amount}, TraceId: {TraceId}",
            userId, amount, traceId);
        return Results.Ok(ApiResponse<AmountResponse>.Ok(new AmountResponse(amount), null, traceId));
    }

    private record AmountResponse(int Amount);
}