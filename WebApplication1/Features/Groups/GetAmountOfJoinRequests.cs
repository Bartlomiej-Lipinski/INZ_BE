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
        app.MapGet("/groups/join-requests/amount", Handle)
            .WithName("GetAmountOfJoinRequests")
            .WithDescription("Returns the amount of join requests for the current user")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    public static async Task<IResult> Handle(
        [FromBody] AmountRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("User ID cannot be null or empty."));
        }

        var adminGroupIds = await dbContext.GroupUsers
            .Where(gu => gu.UserId == request.UserId && gu.IsAdmin)
            .Select(gu => gu.GroupId)
            .ToListAsync(cancellationToken);
        var amount = await dbContext.GroupUsers
            .AsNoTracking()
            .Where(gu => adminGroupIds.Contains(gu.GroupId) && gu.AcceptanceStatus == AcceptanceStatus.Pending && !gu.IsAdmin)
            .CountAsync(cancellationToken);
        

        return Results.Ok(ApiResponse<AmountResponse>.Ok(new AmountResponse(amount)));
    }
    public record AmountRequest(string UserId);
    public record AmountResponse(int Amount);
}