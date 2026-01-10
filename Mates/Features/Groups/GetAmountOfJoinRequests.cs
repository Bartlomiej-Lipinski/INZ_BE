using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Groups;

public class GetAmountOfJoinRequests : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/join-requests/amount", Handle)
            .WithName("GetAmountOfJoinRequests")
            .WithDescription("Returns the amount of join requests for the current user")
            .WithTags("Groups")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }
    
    public static async Task<IResult> Handle(
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetAmountOfJoinRequests> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Fetching pending join requests for admin {UserId}. TraceId: {TraceId}", 
            userId, traceId);

        var adminGroupIds = await dbContext.GroupUsers
            .Where(gu => gu.UserId == userId && gu.IsAdmin)
            .Select(gu => gu.GroupId)
            .ToListAsync(cancellationToken);
        
        var amount = await dbContext.GroupUsers
            .AsNoTracking()
            .Where(gu => adminGroupIds.Contains(gu.GroupId) 
                         && gu.AcceptanceStatus == AcceptanceStatus.Pending && !gu.IsAdmin)
            .CountAsync(cancellationToken);
        
        logger.LogInformation("Pending join requests fetched for user {UserId}. Amount: {Amount}, TraceId: {TraceId}",
            userId, amount, traceId);
        return Results.Ok(ApiResponse<AmountResponse>.Ok(new AmountResponse(amount), null, traceId));
    }

    private record AmountResponse(int Amount);
}