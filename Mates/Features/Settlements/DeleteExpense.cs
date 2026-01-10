using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Infrastructure.Service;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Settlements;

public class DeleteExpense : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/expenses/{expenseId}", Handle)
            .WithName("DeleteExpense")
            .WithDescription("Deletes a specific expense from a group")
            .WithTags("Settlements")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
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
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} attempting to delete expense {ExpenseId} in group {GroupId}. TraceId: {TraceId}",
            userId, expenseId, groupId, traceId);
        
        var expense = await dbContext.Expenses
            .SingleOrDefaultAsync(e => e.Id == expenseId, cancellationToken);
        
        if (expense == null)
        {
            logger.LogWarning("Expense {ExpenseId} not found in group {GroupId}. TraceId: {TraceId}", expenseId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Expense not found.", traceId));
        }
        
        var groupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = groupUser?.IsAdmin ?? false;
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