using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Groups;

public class GetIsAdmin : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/group/{groupId}/isAdmin", Handle)
            .WithName("GetIsGroupAdmin")
            .WithDescription("Checks if User is Admin of given group")
            .WithTags("Groups")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext context,
        ClaimsPrincipal currentUser,
        ILogger<GetIsAdmin> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("Checking admin status for user ID: {UserId}. TraceId: {TraceId}", userId, traceId);

        var userIsAdmin = await context.GroupUsers
            .AsNoTracking()
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == userId && gu.IsAdmin, cancellationToken);

        logger.LogInformation("Admin status for user ID: {UserId} is {IsAdmin}. TraceId: {TraceId}", 
            userId, userIsAdmin, traceId);

        return Results.Ok(ApiResponse<bool>.Ok(userIsAdmin));
    }
}