using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
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

    public static async Task<ApiResponse<IEnumerable<GroupResponseDto>>> Handle(
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<IEnumerable<GroupResponseDto>>.Fail("Unauthorized", traceId);
        }

        var groups = await dbContext.GroupUsers.AsNoTracking()
            .AsQueryable()
            .Where(c => c.UserId == userId)
            .Select(c => new GroupResponseDto
            {
                Id = c.GroupId,
                Name = c.Group.Name
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IEnumerable<GroupResponseDto>>.Ok(groups, null, traceId);
    }
}