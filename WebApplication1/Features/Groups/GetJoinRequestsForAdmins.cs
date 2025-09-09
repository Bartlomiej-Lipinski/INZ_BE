using System.Security.Claims;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GetJoinRequestsForAdmins : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/join-requests/admins", Handle)
            .WithName("GetJoinRequestsForAdmins")
            .WithDescription("Returns join requests for group admins")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        ClaimsPrincipal currentUser, 
        AppDbContext dbContext, 
        CancellationToken cancellationToken,
        HttpContext httpContext,
        ILogger<GetJoinRequestsForAdmins> logger)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;
        
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
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
            .Where(gu => adminGroupIds.Contains(gu.GroupId) && gu.AcceptanceStatus == AcceptanceStatus.Pending && !gu.IsAdmin)
            .Include(gu => gu.Group)
            .Include(gu => gu.User)
            .Select(gu => new SingleJoinRequestResponse(gu.GroupId, gu.Group.Name, gu.UserId, gu.User.UserName))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {RequestCount} pending join requests for admin user: {UserId}. TraceId: {TraceId}", 
            pendingRequests.Count, userId, traceId);

        return Results.Ok(ApiResponse<IEnumerable<SingleJoinRequestResponse>>.Ok(pendingRequests, null, traceId));
    }
    
    public record SingleJoinRequestResponse(string GroupId, string GroupName, string UserId, string? UserName);
}