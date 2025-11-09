using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

[ApiExplorerSettings(GroupName = "Groups")]
public class GetUserGroups : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/groups",Handle)
            .WithName("GetUserGroups")
            .WithDescription("Returns groups for the currently logged-in user")
            .WithTags("Groups");
    }

    public static async Task<IResult> Handle(
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        var groups = await dbContext.GroupUsers.AsNoTracking()
            .AsQueryable()
            .Where(c => c.UserId == userId && c.AcceptanceStatus == AcceptanceStatus.Accepted)
            .Select(c => new GroupResponseDto
            {
                Id = c.GroupId,
                Name = c.Group.Name
            })
            .ToListAsync(cancellationToken);
        
        if (groups.Count == 0)
            return Results.Ok(ApiResponse<List<GroupResponseDto>>
                .Ok(groups, "No groups found for this user.", traceId));

        return Results.Ok(ApiResponse<List<GroupResponseDto>>
            .Ok(groups, "Groups retrieved successfully.", traceId));
    }
}