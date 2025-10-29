using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

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
        ISettlementCalculator settlementCalculator,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to delete expense. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));

        if (group.GroupUsers.All(gu => gu.UserId != userId))
            return Results.Forbid();
        
        var expense = await dbContext.Expenses
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        
        if (expense == null)
        {
            logger.LogWarning("Expense {EventId} not found in group {GroupId}. TraceId: {TraceId}", id, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Event not found.", traceId));
        }
        
        dbContext.Expenses.Remove(expense);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        await settlementCalculator.RecalculateSettlementsForExpenseChangeAsync(
            expense, dbContext, groupId, isAddition: false, logger, cancellationToken);
        
        logger.LogInformation("User {UserId} deleted expense {ExpenseId} from group {GroupId}. TraceId: {TraceId}",
            userId, id, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Expense deleted successfully.", id, traceId));
    }
}