using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Settlements;

public class GetExpenseById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/expenses/{id}", Handle)
            .WithName("GetExpenseById")
            .WithDescription("Retrieves a single expense by its ID")
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
        ILogger<PostExpense> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to retrieve an expense. TraceId: {TraceId}", traceId);
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
            .Include(e => e.Group)
            .Include(e => e.PaidByUser)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (expense == null)
        {
            logger.LogWarning("Expense not found: {RecommendationId}. TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Expense not found.", traceId));
        }
        
        var beneficiaries = await dbContext.ExpenseBeneficiaries
            .AsNoTracking()
            .Include(b => b.User)
            .Where(b => b.ExpenseId == id)
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