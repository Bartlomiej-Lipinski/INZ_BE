using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Settlements.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Settlements;
using Mates.Infrastructure.Service;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mates.Features.Settlements;

public class UpdateExpense : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/expenses/{expenseId}", Handle)
            .WithName("UpdateExpense")
            .WithDescription("Updates an existing expense in a group")
            .WithTags("Settlements")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string expenseId,
        [FromBody] ExpenseRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateExpense> logger,
        [FromServices] ISettlementCalculator settlementCalculator,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        logger.LogInformation("Updating expense {ExpenseId} in group {GroupId}. TraceId: {TraceId}", 
            expenseId, groupId, traceId);
        
        if (string.IsNullOrWhiteSpace(request.PaidByUserId) ||
            string.IsNullOrWhiteSpace(request.Title) ||
            request.Beneficiaries.Count == 0)
        {
            logger.LogWarning("Invalid request data. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail(
                "PaidByUserId, expense title, and beneficiaries are required.", traceId));
        }

        var expense = await dbContext.Expenses
            .Include(e => e.Beneficiaries)
            .SingleOrDefaultAsync(e => e.Id == expenseId && e.GroupId == groupId, cancellationToken);

        if (expense == null)
        {
            logger.LogWarning("Expense not found: {ExpenseId}. TraceId: {TraceId}", expenseId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Expense not found.", traceId));
        }
        
        var oldExpenseSnapshot = new Expense
        {
            Id = expense.Id,
            GroupId = expense.GroupId,
            PaidByUserId = expense.PaidByUserId,
            Title = expense.Title,
            Amount = expense.Amount,
            PhoneNumber = expense.PhoneNumber,
            BankAccount = expense.BankAccount,
            IsEvenSplit = expense.IsEvenSplit,
            CreatedAt = expense.CreatedAt,
            Beneficiaries = expense.Beneficiaries
                .Select(b => new ExpenseBeneficiary
                {
                    ExpenseId = b.ExpenseId,
                    UserId = b.UserId,
                    Share = b.Share
                }).ToList()
        };

        expense.PaidByUserId = request.PaidByUserId;
        expense.Title = request.Title;
        expense.Amount = request.Amount;
        expense.PhoneNumber = request.PhoneNumber;
        expense.BankAccount = request.BankAccount;
        expense.IsEvenSplit = request.IsEvenSplit;

        List<ExpenseBeneficiary> newBeneficiaries;
        if (request.IsEvenSplit)
        {
            var share = Math.Round(request.Amount / request.Beneficiaries.Count, 2, MidpointRounding.AwayFromZero);
            var total = share * request.Beneficiaries.Count;
            var adjustment = Math.Round(request.Amount - total, 2, MidpointRounding.AwayFromZero);

            newBeneficiaries = request.Beneficiaries.Select((b, i) => new ExpenseBeneficiary
            {
                ExpenseId = expense.Id,
                UserId = b.UserId,
                Share = i == request.Beneficiaries.Count - 1 ? share + adjustment : share
            }).ToList();
        }
        else
        {
            if (request.Beneficiaries.Any(b => b.Share == null))
            {
                logger.LogWarning("Missing share for beneficiaries. TraceId: {TraceId}", traceId);
                return Results.BadRequest(ApiResponse<string>
                    .Fail("User's shares should be included since the expense is not evenly split.", traceId));
            }

            var totalShares = request.Beneficiaries.Sum(b => b.Share);
            if (totalShares != request.Amount)
            {
                logger.LogWarning("Sum of shares {TotalShares} != expense amount {Amount}. TraceId: {TraceId}",
                    totalShares, request.Amount, traceId);
                return Results.BadRequest(ApiResponse<string>
                    .Fail("Sum of beneficiaries shares does not equal the expense amount.", traceId));
            }

            newBeneficiaries = request.Beneficiaries.Select(b => new ExpenseBeneficiary
            {
                ExpenseId = expense.Id,
                UserId = b.UserId,
                Share = (decimal)b.Share!
            }).ToList();
        }
        
        await settlementCalculator.RecalculateSettlementsForExpenseChangeAsync(
            oldExpenseSnapshot, dbContext, groupId, isAddition: false, logger, cancellationToken);

        await settlementCalculator.RecalculateSettlementsForExpenseChangeAsync(
            expense, dbContext, groupId, isAddition: true, logger, cancellationToken);

        dbContext.ExpenseBeneficiaries.RemoveRange(expense.Beneficiaries);
        expense.Beneficiaries = newBeneficiaries;
        await dbContext.ExpenseBeneficiaries.AddRangeAsync(newBeneficiaries, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Expense {ExpenseId} updated successfully in group {GroupId}. TraceId: {TraceId}",
            expenseId, groupId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Expense updated successfully.", expenseId, traceId));
    }
}