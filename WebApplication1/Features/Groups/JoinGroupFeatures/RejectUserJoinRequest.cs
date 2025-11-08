using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups.JoinGroupFeatures;

public class RejectUserJoinRequest : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/reject-join-request/", Handle)
            .WithName("RejectUserJoinRequest")
            .WithDescription("Rejects a user's join request to a group")
            .WithTags("Groups")
            .RequireAuthorization();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] RejectUserJoinRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        ILogger<RejectUserJoinRequest> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;
        
        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }
        
        var currentGroupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(
                gu => gu.GroupId == groupId 
                      && gu.UserId == userId 
                      && gu.AcceptanceStatus == AcceptanceStatus.Accepted, cancellationToken);

        var isAdmin = currentGroupUser?.IsAdmin == true;
        if (!isAdmin)
        {
            logger.LogWarning("User {UserId} is not admin of group {GroupId}. TraceId: {TraceId}", 
                userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>
                .Fail("Only group admin can reject join requests.", traceId));
        }
        
        var groupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == request.UserId, cancellationToken);

        if (groupUser == null)
        {
            logger.LogWarning("Join request not found. GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}",
                groupId, request.UserId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Join request not found.", traceId));
        }

        if (groupUser.AcceptanceStatus != AcceptanceStatus.Pending)
        {
            logger.LogWarning("Join request is not pending. GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}",
                groupId, request.UserId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Join request is not pending.", traceId));
        }

        dbContext.GroupUsers.Remove(groupUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Join request rejected successfully. " +
                              "GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}", 
            groupId, request.UserId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Join request rejected successfully.", null, traceId));
    }
}