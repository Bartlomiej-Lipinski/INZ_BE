using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Groups.Dtos;
using Mates.Features.Storage.Dtos;
using Mates.Features.Users.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Groups.JoinGroupFeatures;

public class GetJoinRequestsForAdmins : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/join-requests/admins", Handle)
            .WithName("GetJoinRequestsForAdmins")
            .WithDescription("Returns join requests for group admins")
            .WithTags("Groups")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetJoinRequestsForAdmins> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Fetching join requests for admin user: {UserId}. TraceId: {TraceId}", userId, traceId);

        var adminGroupIds = await dbContext.GroupUsers
            .AsNoTracking()
            .Where(gu => gu.UserId == userId && gu.IsAdmin)
            .Select(gu => gu.GroupId)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {GroupCount} groups where user {UserId} is admin. TraceId: {TraceId}",
            adminGroupIds.Count, userId, traceId);

        var pendingRequests = await dbContext.GroupUsers
            .AsNoTracking()
            .Where(gu => adminGroupIds.Contains(gu.GroupId) && gu.AcceptanceStatus == AcceptanceStatus.Pending &&
                         !gu.IsAdmin)
            .Include(gu => gu.Group)
            .Include(gu => gu.User)
            .ToListAsync(cancellationToken);

        var userIds = pendingRequests.Select(c => c.UserId).Distinct().ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);

        var response = pendingRequests.Select(gu => new JoinRequestResponseDto
        {
            GroupId = gu.GroupId,
            GroupName = gu.Group.Name,
            User = new UserResponseDto
            {
                Id = gu.UserId,
                Name = gu.User.Name,
                Surname = gu.User.Surname,
                Username = gu.User.UserName,
                ProfilePicture = profilePictures.TryGetValue(gu.UserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null
            }
        })
        .ToList();

        logger.LogInformation("Found {RequestCount} pending join requests for admin user: {UserId}. TraceId: {TraceId}",
            pendingRequests.Count, userId, traceId);

        return Results.Ok(ApiResponse<IEnumerable<JoinRequestResponseDto>>.Ok(response, null, traceId));
    }
}