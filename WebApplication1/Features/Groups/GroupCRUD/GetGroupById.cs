using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups.GroupCRUD;

[ApiExplorerSettings(GroupName = "Groups")]
public class GetGroupById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{id}", Handle)
            .WithName("GetGroupById")
            .WithDescription("Returns a group by ID")
            .WithTags("Groups")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string id,
        AppDbContext dbContext,
        HttpContext httpContext,
        ClaimsPrincipal currentUser,
        ILogger<GetGroupById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogWarning("Invalid group ID provided. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Group ID cannot be null or empty.", traceId));
        }

        logger.LogInformation("Fetching group with ID: {GroupId}. TraceId: {TraceId}", id, traceId);

        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group not found with ID: {GroupId}. TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found", traceId));
        }
        
        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to delete event in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, id, traceId);
            return Results.Forbid();
        }
        
        var dto = new GroupResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Color = group.Color,
            Code = group.Code
        };

        logger.LogInformation("Group successfully retrieved with ID: {GroupId}. TraceId: {TraceId}", id, traceId);
        return Results.Ok(ApiResponse<GroupResponseDto>.Ok(dto, "Group retrieved successfully", traceId));
    }
}