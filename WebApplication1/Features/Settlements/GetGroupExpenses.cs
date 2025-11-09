using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Settlements;

public class GetGroupExpenses : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/expenses", Handle)
            .WithName("GetGroupExpenses")
            .WithDescription("Retrieves all expenses for a specific group")
            .WithTags("Settlements")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupExpenses> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var expenses = await dbContext.Expenses
            .AsNoTracking()
            .Include(e => e.Beneficiaries)
            .Where(e => e.GroupId == groupId)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new ExpenseResponseDto
            {
                Id = e.Id,
                PaidByUserId = e.PaidByUserId,
                Title = e.Title,
                Amount = e.Amount,
                PhoneNumber = e.PhoneNumber,
                BankAccount = e.BankAccount,
                IsEvenSplit = e.IsEvenSplit,
                CreatedAt = e.CreatedAt.ToLocalTime(),
                Beneficiaries = e.Beneficiaries.Select(b => new ExpenseBeneficiaryDto
                {
                    UserId = b.UserId,
                    Share = b.Share,
                }).ToList()
            })
            .ToListAsync(cancellationToken);
        
        if (expenses.Count == 0)
            return Results.Ok(ApiResponse<List<ExpenseResponseDto>>
                .Ok(expenses, "No expenses found for this group.", traceId));

        return Results.Ok(ApiResponse<List<ExpenseResponseDto>>
            .Ok(expenses, "Group expenses retrieved successfully.", traceId));
    }
}