using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class DeleteUserFromGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/users/{userId}", Handle)
            .WithName("DeleteUserFromGroup")
            .WithDescription("Deletes a user from a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string userId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        ILogger<DeleteUserFromGroup> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogInformation("Deleting user {UserId} from group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);
        
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to delete user from group. TraceId: {TraceId}", traceId);
        }
        var isCurrentUserAdmin = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == currentUserId && gu.IsAdmin, cancellationToken);
        if (!isCurrentUserAdmin)
        {
            logger.LogWarning("User {CurrentUserId} is not admin of group {GroupId}. TraceId: {TraceId}",
                currentUserId, groupId, traceId);
            return Results.Forbid();
        }
        var groupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == userId, cancellationToken);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} not found in group {GroupId}. TraceId: {TraceId}",
                userId, groupId, traceId);
            return Results.NotFound();
        }
        dbContext.GroupUsers.Remove(groupUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return Results.Ok(ApiResponse<string>.Ok("User removed from group successfully.", traceId));
    }
}