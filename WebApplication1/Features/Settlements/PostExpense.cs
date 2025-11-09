using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Settlements;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Settlements;

public class PostExpense : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/expenses", Handle)
            .WithName("PostExpense")
            .WithDescription("Creates a new expense within a group by a member")
            .WithTags("Settlements")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] ExpenseRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostExpense> logger,
        [FromServices] ISettlementCalculator settlementCalculator,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Creating expense in group {GroupId} by user {UserId}. TraceId: {TraceId}", 
            groupId, userId, traceId);

        if (string.IsNullOrWhiteSpace(request.PaidByUserId) || string.IsNullOrWhiteSpace(request.Title)
                                                            || request.Beneficiaries.Count == 0)
        {
            logger.LogWarning("Invalid expense request. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>
                .Fail("PaidByUserId, expense title and beneficiaries are required.", traceId));
        }

        var expense = new Expense
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            PaidByUserId = request.PaidByUserId,
            Title = request.Title,
            Amount = request.Amount,
            PhoneNumber = request.PhoneNumber,
            BankAccount = request.BankAccount,
            IsEvenSplit = request.IsEvenSplit,
            CreatedAt = DateTime.UtcNow
        };

        if (request.IsEvenSplit)
        {
            var share = Math.Round(request.Amount / request.Beneficiaries.Count, 2, MidpointRounding.AwayFromZero);
            var total = share * request.Beneficiaries.Count;
            var adjustment = Math.Round(request.Amount - total, 2, MidpointRounding.AwayFromZero);

            expense.Beneficiaries = request.Beneficiaries.Select((b, i) => new ExpenseBeneficiary
            {
                ExpenseId = expense.Id,
                UserId = b.UserId,
                Share = i == request.Beneficiaries.Count - 1 ? share + adjustment : share
            }).ToList();
        }
        else
        {
            if (request.Beneficiaries.Any(expenseBeneficiary => expenseBeneficiary.Share == null))
            {
                logger.LogWarning("Missing share for some beneficiaries. TraceId: {TraceId}", traceId);
                return Results.BadRequest(ApiResponse<string>
                    .Fail("User's shares should be included since the expense is not evenly split.", traceId));
            }

            var totalShares = request.Beneficiaries.Sum(b => b.Share);
            if (totalShares != request.Amount)
            {
                logger.LogWarning("Sum of shares {TotalShares} does not equal expense amount {Amount}. TraceId: {TraceId}", 
                    totalShares, request.Amount, traceId);
                return Results.BadRequest(ApiResponse<string>
                    .Fail("Sum of beneficiaries shares does not equal the expense amount.", traceId));
            }

            expense.Beneficiaries = request.Beneficiaries.Select(b => new ExpenseBeneficiary
            {
                ExpenseId = expense.Id,
                UserId = b.UserId,
                Share = (decimal)b.Share!
            }).ToList();
        }

        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        await settlementCalculator.RecalculateSettlementsForExpenseChangeAsync(
            expense, dbContext, groupId, isAddition: true, logger, cancellationToken);

        logger.LogInformation(
            "User {UserId} added new expense {ExpenseId} in group {GroupId}. TraceId: {TraceId}",
            userId, expense.Id, groupId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Expense created successfully.", expense.Id, traceId));
    }
}