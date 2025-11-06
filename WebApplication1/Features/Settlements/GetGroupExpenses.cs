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

public class GetGroupExpenses : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/expenses", Handle)
            .WithName("GetGroupExpenses")
            .WithDescription("Retrieves all expenses for a specific group")
            .WithTags("Settlements")
            .RequireAuthorization();
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
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to get group expenses. TraceId: {TraceId}", traceId);
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

        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to get group expenses in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

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

        return Results.Ok(ApiResponse<List<ExpenseResponseDto>>
            .Ok(expenses, "Group expenses retrieved successfully.", traceId));
    }
}