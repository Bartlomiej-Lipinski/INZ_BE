using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;

namespace WebApplication1.Features.Settlements;

public class DeleteExpense : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/expenses/{id}", Handle)
            .WithName("DeleteExpense")
            .WithDescription("Deletes a specific expense from a group")
            .WithTags("Settlements")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string id,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteExpense> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to delete expense. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
    }
}