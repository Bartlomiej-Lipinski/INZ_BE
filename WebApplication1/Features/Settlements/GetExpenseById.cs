using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Settlements;

public class GetExpenseById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/expenses/{expenseId}", Handle)
            .WithName("GetExpenseById")
            .WithDescription("Retrieves a single expense by its ID")
            .WithTags("Settlements")
            .RequireAuthorization();
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
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;
        
        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to retrieve an expense in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
        
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
        
        return Results.Ok(ApiResponse<ExpenseResponseDto>.Ok(response, null, traceId));
    }
}