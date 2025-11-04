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
        app.MapDelete("/groups/{groupId}/expenses/{expenseId}", Handle)
            .WithName("DeleteExpense")
            .WithDescription("Deletes a specific expense from a group")
            .WithTags("Settlements")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string expenseId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteExpense> logger,
        [FromServices] ISettlementCalculator settlementCalculator,
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
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to delete expense in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
        
        var expense = await dbContext.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId, cancellationToken);
        
        if (expense == null)
        {
            logger.LogWarning("Expense {ExpenseId} not found in group {GroupId}. TraceId: {TraceId}", expenseId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Expense not found.", traceId));
        }
        
        var isAdmin = groupUser.IsAdmin;
        if (expense.PaidByUserId != userId && !isAdmin)
        {
            logger.LogWarning("User {UserId} attempted to delete expense {ExpenseId} they do not own and is not admin. " +
                              "TraceId: {TraceId}", userId, expenseId, traceId);
            return Results.Forbid();
        }
        
        dbContext.Expenses.Remove(expense);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        await settlementCalculator.RecalculateSettlementsForExpenseChangeAsync(
            expense, dbContext, groupId, isAddition: false, logger, cancellationToken);
        
        logger.LogInformation("User {UserId} deleted expense {ExpenseId} from group {GroupId}. TraceId: {TraceId}",
            userId, expenseId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Expense deleted successfully.", expenseId, traceId));
    }
}