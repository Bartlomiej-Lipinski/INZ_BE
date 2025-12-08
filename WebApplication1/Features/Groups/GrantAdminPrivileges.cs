using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Groups;

public class GrantAdminPrivileges : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/grant-admin-privileges", Handle)
            .WithName("GrantAdminPrivileges")
            .WithDescription("Grants admin Privileges to a user in a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] GrantAdminPrivilegesDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GrantAdminPrivileges> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.GetUserId();
        
        logger.LogInformation("Processing admin privilege grant. " +
                              "GroupId: {GroupId}, UserId: {UserId}, AdminId: {AdminId}. TraceId: {TraceId}", 
            groupId, request.UserId, currentUserId, traceId);
        
        var currentGroupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = currentGroupUser?.IsAdmin ?? false;
        
        if (!isAdmin)
        {
            logger.LogWarning("User {UserId} is not admin of group {GroupId}. TraceId: {TraceId}", 
                currentUserId, groupId, traceId);
            return Results.Forbid();
        }
        
        var groupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(
                gu => gu.GroupId==groupId 
                      && gu.UserId == request.UserId 
                      && gu.AcceptanceStatus == AcceptanceStatus.Accepted, cancellationToken);

        if (groupUser == null)
        {
            logger.LogWarning("Target user {UserId} not found in group {GroupId}. TraceId: {TraceId}",
                request.UserId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("User not found in group.", traceId));
        }

        if (groupUser.IsAdmin)
        {
            logger.LogInformation("User {TargetUserId} already has admin privileges in group {GroupId}. TraceId: {TraceId}",
                request.UserId, groupId, traceId);
            return Results.Ok(ApiResponse<string>.Fail("User already has admin privileges.", traceId));
        }

        groupUser.IsAdmin = true;
        dbContext.GroupUsers.Update(groupUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin privileges granted to user {TargetUserId} in group {GroupId}. TraceId: {TraceId}",
            request.UserId, groupId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Admin privileges granted successfully.", traceId));
    }
}