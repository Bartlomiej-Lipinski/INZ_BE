using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Settlements.Dtos;
using Mates.Features.Storage.Dtos;
using Mates.Features.Users.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Settlements;

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
        
        var userIds = beneficiaries.Select(c => c.UserId)
            .Append(expense.PaidByUserId).Distinct().ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);

        var response = new ExpenseResponseDto
        {
            Id = expense.Id,
            GroupId = expense.GroupId,
            PaidByUser = new UserResponseDto
            {
                Id = expense.PaidByUserId,
                Name = expense.PaidByUser.Name,
                Surname = expense.PaidByUser.Surname,
                Username = expense.PaidByUser.UserName,
                ProfilePicture = profilePictures.TryGetValue(expense.PaidByUserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null  
            },
            Title = expense.Title,
            Amount = expense.Amount,
            PhoneNumber = expense.PhoneNumber,
            BankAccount = expense.BankAccount,
            IsEvenSplit = expense.IsEvenSplit,
            CreatedAt = expense.CreatedAt.ToLocalTime(),
            Beneficiaries = beneficiaries.Select(b => new ExpenseBeneficiaryDto
            {
                User = new UserResponseDto
                {
                    Id = b.UserId,
                    Name = b.User.Name,
                    Surname = b.User.Surname,
                    Username = b.User.UserName,
                    ProfilePicture = profilePictures.TryGetValue(b.UserId, out var beneficioariesPhoto)
                        ? new ProfilePictureResponseDto
                        {
                            Url = beneficioariesPhoto.Url,
                            FileName = beneficioariesPhoto.FileName,
                            ContentType = beneficioariesPhoto.ContentType,
                            Size = beneficioariesPhoto.Size
                        }
                        : null  
                },
                Share = b.Share
            }).ToList()
        };
        
        logger.LogInformation("Expense {ExpenseId} retrieved successfully for group {GroupId}. TraceId: {TraceId}", 
            expenseId, groupId, traceId);
        return Results.Ok(ApiResponse<ExpenseResponseDto>.Ok(response, null, traceId));
    }
}