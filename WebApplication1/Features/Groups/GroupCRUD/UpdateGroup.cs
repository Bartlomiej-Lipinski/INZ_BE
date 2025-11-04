using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups.GroupCRUD;

public class UpdateGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}", Handle)
            .WithName("UpdateGroup")
            .WithDescription("Updates group details")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] GroupRequestDto request,
        AppDbContext dbContext,
        HttpContext httpContext,
        ClaimsPrincipal currentUser,
        ILogger<UpdateGroup> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to update group. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group not found. GroupId: {GroupId}, TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }
        
        var groupUser = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to update group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
 
        group.Name = request.Name;
        group.Color = request.Color;

        dbContext.Groups.Update(group);
        var updated = await dbContext.SaveChangesAsync(cancellationToken);

        if (updated > 0)
        {
            logger.LogInformation("Group updated successfully. GroupId: {GroupId}, TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<string>.Ok("Group updated successfully.", "Group Updated", traceId));
        }

        logger.LogError("Failed to update group. GroupId: {GroupId}, TraceId: {TraceId}", groupId, traceId);
        return Results.Json(ApiResponse<string>.Fail("Failed to update group.", traceId), statusCode: 500);
    }
}