using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Settlements;

public class GetUserSettlements :IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/settlements", Handle)
            .WithName("GetUserSettlements")
            .WithDescription("Retrieves all settlements for current user for a specific group")
            .WithTags("Settlements")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetUserSettlements> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Retrieving settlements for user {UserId} in group {GroupId}. TraceId: {TraceId}", 
            userId, groupId, traceId);
        
        var settlements = await dbContext.Settlements
            .AsNoTracking()
            .Include(s => s.Group)
            .Include(s => s.ToUser)
            .Where(s => s.GroupId == groupId && s.FromUserId == userId)
            .ToListAsync(cancellationToken);
        
        var userIds = settlements.Select(c => c.ToUserId).ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);

        var response = settlements.Select(s => new SettlementResponseDto
        {
            Id = s.Id,
            GroupId = s.GroupId,
            ToUser = new UserResponseDto
            {
                Id = s.ToUserId,
                Name = s.ToUser.Name,
                Surname = s.ToUser.Surname,
                Username = s.ToUser.UserName,
                ProfilePicture = profilePictures.TryGetValue(s.ToUserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null  
            },
            Amount = s.Amount,

        })
        .ToList();
        
        if (settlements.Count == 0)
        {
            logger.LogInformation("No settlements found for user {UserId} in group {GroupId}. TraceId: {TraceId}", 
                userId, groupId, traceId);
            return Results.Ok(ApiResponse<List<SettlementResponseDto>>
                .Ok(response, "No settlements found for this user.", traceId));
        }
        
        logger.LogInformation("Retrieved {Count} settlements for user {UserId} in group {GroupId}. TraceId: {TraceId}", 
            settlements.Count, userId, groupId, traceId);
        return Results.Ok(ApiResponse<List<SettlementResponseDto>>
            .Ok(response, "User settlements retrieved successfully.", traceId));
    }
}