using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            return Results.Unauthorized();
        }
        var currentGroupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == request.GroupId && gu.UserId == currentUserId, cancellationToken);

        bool isAdmin = currentGroupUser?.IsAdmin == true;
        if (!isAdmin)
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Only group admin can reject join requests."));
        }
        
        
        var groupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == request.GroupId && gu.UserId == request.UserId, cancellationToken);

        if (groupUser == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Join request not found."));
        }

        if (groupUser.AcceptanceStatus != AcceptanceStatus.Pending)
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Join request is not pending."));
        }

        dbContext.GroupUsers.Remove(groupUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ApiResponse<string>.Ok("Join request rejected successfully."));
    }
    public record RejectUserJoinRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string GroupId { get; set; }
        [Required]
        [MaxLength(50)]
        public string UserId { get; set; } 
    }
}