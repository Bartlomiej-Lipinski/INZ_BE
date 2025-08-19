using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class RejectUserJoinRequest : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/reject-join-request/{userId}", Handle)
            .WithName("RejectUserJoinRequest")
            .WithDescription("Rejects a user's join request to a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    public static async Task<IResult> Handle(
        [FromBody] RejectUserJoinRequestDto request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var groupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == request.GroupId && gu.UserId == request.UserId, cancellationToken);

        if (groupUser == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Join request not found."));
        }

        if (groupUser.Accepted != AcceptanceStatus.Pending)
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Join request is not pending."));
        }

        dbContext.GroupUsers.Remove(groupUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ApiResponse<string>.Ok("Join request rejected successfully."));
    }
    public record RejectUserJoinRequestDto
    {
        public string GroupId { get; init; } = null!;
        public string UserId { get; init; } = null!;
    }
}