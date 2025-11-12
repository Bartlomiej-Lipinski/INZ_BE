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
        app.MapGet("/users/groups", Handle)
            .WithName("GetUserGroups")
            .WithDescription("Returns groups for the currently logged-in user")
            .WithTags("Groups")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<IEnumerable<GroupResponseDto>>> Handle(
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.GetUserId();

        var groups = await dbContext.GroupUsers.AsNoTracking()
            .AsQueryable()
            .Where(c => c.UserId == currentUserId && c.AcceptanceStatus == AcceptanceStatus.Accepted)
            .Select(c => new GroupResponseDto
            {
                Id = c.GroupId,
                Name = c.Group.Name,
                Color = c.Group.Color
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IEnumerable<GroupResponseDto>>.Ok(groups, null, traceId);
    }
}