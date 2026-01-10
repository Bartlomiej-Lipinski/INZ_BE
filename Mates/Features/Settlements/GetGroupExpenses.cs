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

namespace Mates.Features.Settlements;

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
        logger.LogInformation("Retrieving expenses for group {GroupId}. TraceId: {TraceId}", groupId, traceId);

        var expenses = await dbContext.Expenses
            .AsNoTracking()
            .Include(e => e.Beneficiaries)
            .ThenInclude(b => b.User).Include(expense => expense.PaidByUser)
            .Where(e => e.GroupId == groupId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var userIds = expenses
            .Select(e => e.PaidByUserId)
            .Concat(expenses.SelectMany(e => e.Beneficiaries).Select(b => b.UserId))
            .Distinct()
            .ToList();

        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);

        var response = expenses.Select(e => new ExpenseResponseDto
            {
                Id = e.Id,
                PaidByUser = new UserResponseDto
                {
                    Id = e.PaidByUserId,
                    Name = e.PaidByUser.Name,
                    Surname = e.PaidByUser.Surname,
                    Username = e.PaidByUser.UserName,
                    ProfilePicture = profilePictures.TryGetValue(e.PaidByUserId, out var photo)
                        ? new ProfilePictureResponseDto
                        {
                            Id = photo.Id,
                            Url = photo.Url,
                            FileName = photo.FileName,
                            ContentType = photo.ContentType,
                            Size = photo.Size
                        }
                        : null
                },
                Title = e.Title,
                Amount = e.Amount,
                PhoneNumber = e.PhoneNumber,
                BankAccount = e.BankAccount,
                IsEvenSplit = e.IsEvenSplit,
                CreatedAt = e.CreatedAt.ToLocalTime(),
                Beneficiaries = e.Beneficiaries.Select(b => new ExpenseBeneficiaryDto
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
                                Id = beneficioariesPhoto.Id,
                                Url = beneficioariesPhoto.Url,
                                FileName = beneficioariesPhoto.FileName,
                                ContentType = beneficioariesPhoto.ContentType,
                                Size = beneficioariesPhoto.Size
                            }
                            : null
                    },
                    Share = b.Share
                }).ToList()
            })
            .ToList();

        if (expenses.Count == 0)
        {
            logger.LogInformation("No expenses found for group {GroupId}. TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<List<ExpenseResponseDto>>
                .Ok(response, "No expenses found for this group.", traceId));
        }

        logger.LogInformation("Retrieved {Count} expenses for group {GroupId}. TraceId: {TraceId}",
            expenses.Count, groupId, traceId);
        return Results.Ok(ApiResponse<List<ExpenseResponseDto>>
            .Ok(response, "Group expenses retrieved successfully.", traceId));
    }
}