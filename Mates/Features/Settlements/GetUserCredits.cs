using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Settlements.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Settlements;

public class GetUserCredits : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/credits", Handle)
            .WithName("GetUserCredits")
            .WithDescription("Retrieves how much the current user is owed by others in a group")
            .WithTags("Settlements")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetUserCredits> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation(
            "Retrieving credits for user {UserId} in group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);

        var totalAmount = await dbContext.Settlements
            .AsNoTracking()
            .Where(s => s.GroupId == groupId && s.ToUserId == userId)
            .SumAsync(s => s.Amount, cancellationToken);

        var response = new UserCreditResponseDto
        {
            Amount = totalAmount
        };
        
        if (totalAmount == 0)
        {
            return Results.Ok(ApiResponse<UserCreditResponseDto>
                .Ok(response, "No credits found for this user.", traceId));
        }

        return Results.Ok(ApiResponse<UserCreditResponseDto>
            .Ok(response, "User credits retrieved successfully.", traceId));
    }
}