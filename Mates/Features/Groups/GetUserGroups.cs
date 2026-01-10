using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Groups.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Groups;

[ApiExplorerSettings(GroupName = "Groups")]
public class GetUserGroups : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/groups", Handle)
            .WithName("GetUserGroups")
            .WithDescription("Returns groups for the currently logged-in user")
            .WithTags("Groups")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetUserGroups> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Fetching groups for user {UserId}. TraceId: {TraceId}", 
            userId, traceId);

        var groups = await dbContext.GroupUsers.AsNoTracking()
            .AsQueryable()
            .Where(c => c.UserId == userId && c.AcceptanceStatus == AcceptanceStatus.Accepted)
            .Select(c => new GroupResponseDto
            {
                Id = c.GroupId,
                Name = c.Group.Name,
                Color = c.Group.Color
            })
            .ToListAsync(cancellationToken);
        
        if (groups.Count == 0)
        {
            logger.LogInformation("No groups found for user {UserId}. TraceId: {TraceId}", 
                userId, traceId);
            return Results.Ok(ApiResponse<List<GroupResponseDto>>.Ok(groups, "No groups found for this user.", traceId));
        }

        logger.LogInformation("Retrieved {Count} groups for user {UserId}. TraceId: {TraceId}", 
            groups.Count, userId, traceId);
        return Results.Ok(ApiResponse<List<GroupResponseDto>>
            .Ok(groups, "Groups retrieved successfully.", traceId));
    }
}