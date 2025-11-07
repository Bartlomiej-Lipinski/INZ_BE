using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups.JoinGroupFeatures;

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
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to retrieve requests. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
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
            .Select(gu => new JoinRequestResponseDto
            {
                GroupId = gu.GroupId, 
                GroupName = gu.Group.Name,
                UserId = gu.UserId,
                UserName = gu.User.UserName
                
            }).ToListAsync(cancellationToken);


        logger.LogInformation("Found {RequestCount} pending join requests for admin user: {UserId}. TraceId: {TraceId}",
            pendingRequests.Count, userId, traceId);

        return Results.Ok(ApiResponse<IEnumerable<JoinRequestResponseDto>>.Ok(pendingRequests, null, traceId));
    }
}