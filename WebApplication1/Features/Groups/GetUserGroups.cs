using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

[ApiExplorerSettings(GroupName = "Groups")]
public class GetUserGroups : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/{userId}/groups",
                ([FromRoute] string userId, AppDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    var request = new GetUserGroupsRequest(userId);
                    return Handle(request, dbContext, cancellationToken);
                })
            .WithName("GetUserGroups")
            .WithDescription("Returns groups for a given user")
            .WithTags("Groups")
            .WithOpenApi();
    }

    public static async Task<ApiResponse<IEnumerable<GroupResponse>>> Handle(GetUserGroupsRequest request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var groups = await dbContext.GroupUsers.AsNoTracking().Where(c => c.UserId == request.UserId)
            .Select(c => new GroupResponse(c.GroupId, c.Group.Name))
            .ToListAsync(cancellationToken);
        
        return ApiResponse<IEnumerable<GroupResponse>>.Ok(groups);
    }

    public record GetUserGroupsRequest(string UserId);

    public record GroupResponse(string Id, string Name);
}