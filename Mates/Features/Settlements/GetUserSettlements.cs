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