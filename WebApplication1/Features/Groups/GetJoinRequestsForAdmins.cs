using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GetJoinRequestsForAdmins : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/join-requests/admins", Handle)
            .WithName("GetJoinRequestsForAdmins")
            .WithDescription("Returns join requests for group admins")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    public static async Task<IResult> Handle(
        [FromBody] GetJoinRequestsForAdminsRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var adminGroupIds = await dbContext.GroupUsers
            .AsNoTracking()
            .Where(gu => gu.UserId == request.UserId && gu.IsAdmin)
            .Select(gu => gu.GroupId)
            .ToListAsync(cancellationToken);
        var prndingRequests = await dbContext.GroupUsers
            .AsNoTracking()
            .Where(gu => adminGroupIds.Contains(gu.GroupId) && gu.AcceptanceStatus == AcceptanceStatus.Pending && !gu.IsAdmin)
            .Include(gu => gu.Group)
            .Include(gu => gu.User)
            .Select(gu => new SingleJoinRequestResponse(gu.GroupId, gu.Group.Name, gu.UserId, gu.User.UserName))
            .ToListAsync(cancellationToken);


        return Results.Ok(ApiResponse<IEnumerable<SingleJoinRequestResponse>>.Ok(prndingRequests));
    }
    public record GetJoinRequestsForAdminsRequest(string UserId);
    public record SingleJoinRequestResponse(string GroupId, string GroupName, string UserId, string UserName);
}