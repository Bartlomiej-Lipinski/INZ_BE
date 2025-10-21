using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GetIsAdmin : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/group/{groupid}/isAdmin", Handle)
                        .WithName("GetIsGroupAdmin")
            .WithDescription("Checks if User is Admin of given group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupid,
        AppDbContext context,
        ClaimsPrincipal currentUser,
        ILogger<GetIsAdmin> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to check admin status. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        logger.LogInformation("Checking admin status for user ID: {UserId}. TraceId: {TraceId}", userId, traceId);

        var userIsAdmin = await context.GroupUsers
            .AsNoTracking()
            .AnyAsync(gu => gu.GroupId == groupid && gu.UserId == userId && gu.IsAdmin, cancellationToken);

        logger.LogInformation("Admin status for user ID: {UserId} is {IsAdmin}. TraceId: {TraceId}", 
            userId, userIsAdmin, traceId);

        return Results.Ok(ApiResponse<bool>.Ok(userIsAdmin));
    }
}