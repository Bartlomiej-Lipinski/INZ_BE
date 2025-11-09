using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Settlements;

public class GetExpenseById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/expenses/{expenseId}", Handle)
            .WithName("GetExpenseById")
            .WithDescription("Retrieves a single expense by its ID")
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
        ILogger<PostExpense> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("[GetExpenseById] User {UserId} fetching expense {ExpenseId} in group {GroupId}. TraceId: {TraceId}",
            userId, expenseId, groupId, traceId);
        
        var expense = await dbContext.Expenses
            .Include(e => e.Group)
            .Include(e => e.PaidByUser)
            .FirstOrDefaultAsync(e => e.Id == expenseId, cancellationToken);

        if (expense == null)
        {
            logger.LogWarning("Expense not found: {RecommendationId}. TraceId: {TraceId}", expenseId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Expense not found.", traceId));
        }
        
        var beneficiaries = await dbContext.ExpenseBeneficiaries
            .AsNoTracking()
            .Include(b => b.User)
            .Where(b => b.ExpenseId == expenseId)
            .ToListAsync(cancellationToken);

        var response = new ExpenseResponseDto
        {
            Id = expense.Id,
            GroupId = expense.GroupId,
            PaidByUserId = expense.PaidByUserId,
            Title = expense.Title,
            Amount = expense.Amount,
            PhoneNumber = expense.PhoneNumber,
            BankAccount = expense.BankAccount,
            IsEvenSplit = expense.IsEvenSplit,
            CreatedAt = expense.CreatedAt.ToLocalTime(),
            Beneficiaries = beneficiaries.Select(b => new ExpenseBeneficiaryDto
            {
                UserId = b.UserId,
                Share = b.Share
            }).ToList()
        };
        
        logger.LogInformation("Expense {ExpenseId} retrieved successfully for group {GroupId}. TraceId: {TraceId}", 
            expenseId, groupId, traceId);
        return Results.Ok(ApiResponse<ExpenseResponseDto>.Ok(response, null, traceId));
    }
}