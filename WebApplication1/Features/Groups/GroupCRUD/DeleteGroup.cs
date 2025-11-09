using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups.GroupCRUD;

public class DeleteGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}", Handle)
            .WithName("DeleteGroup")
            .WithDescription("Deletes a specific group")
            .WithTags("Groups")
            .RequireAuthorization();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteGroup> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} started deleting group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group not found. GroupId: {GroupId}. TraceId: {TraceId}", 
                groupId, traceId);
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
            return Results.Forbid();
        }

        dbContext.Groups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Group {GroupId} deleted by user {UserId}. TraceId: {TraceId}", 
            groupId, userId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Group deleted successfully.", traceId));
    }
}