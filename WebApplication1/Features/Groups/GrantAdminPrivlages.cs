using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GrantAdminPrivlages : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/grant-admin-privileges", Handle)
            .WithName("GrantAdminPrivileges")
            .WithDescription("Grants admin Privileges to a user in a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    public static async Task<IResult> Handle(
        [FromBody] GrantAdminPrivlagesDto request,
        AppDbContext dbContext,
        ClaimsPrincipal? user,
        HttpContext httpContext,
        ILogger<GrantAdminPrivlages> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        if (user == null)
        {
            logger.LogWarning("Null user principal in GrantAdminPrivlages. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? user.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(currentUserId))
        {
            logger.LogWarning("No user ID found in claims for GrantAdminPrivlages. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        logger.LogInformation("Processing admin privilege grant. " +
                              "GroupId: {GroupId}, UserId: {UserId}, AdminId: {AdminId}. TraceId: {TraceId}", 
            request.GroupId, request.UserId, currentUserId, traceId);
        
        var isCurrentUserAdmin = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == request.GroupId  && gu.UserId == currentUserId && gu.IsAdmin,
                cancellationToken);
        if (!isCurrentUserAdmin)
        {
            logger.LogWarning("User {UserId} is not admin of group {GroupId}. TraceId: {TraceId}", 
                currentUserId, request.GroupId, traceId);
            return Results.Forbid();
        }
        var groupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId.Equals(request.GroupId) && gu.UserId == request.UserId,
                cancellationToken);

        if (groupUser == null)
        {
            logger.LogWarning("Target user {UserId} not found in group {GroupId}. TraceId: {TraceId}", request.UserId, request.GroupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("User not found in group.", traceId));
        }

        if (groupUser.IsAdmin)
        {
            return Results.Ok(ApiResponse<string>.Fail("User already has admin privileges.", traceId));
        }

        groupUser.IsAdmin = true;
        dbContext.GroupUsers.Update(groupUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ApiResponse<string>.Ok("Admin privileges granted successfully.", traceId));
    }
    public record GrantAdminPrivlagesDto(string GroupId, string UserId);
}