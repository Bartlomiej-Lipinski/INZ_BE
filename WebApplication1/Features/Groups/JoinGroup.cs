using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class JoinGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/join", Handle)
            .WithName("JoinGroup")
            .WithDescription("Allows a user to join a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    public static async Task<IResult> Handle(
        [FromBody] JoinGroupRequest request,
        AppDbContext dbContext,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }
        
        if (string.IsNullOrWhiteSpace(request.GroupCode))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Group ID and Code are required."));
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g =>  g.Code == request.GroupCode, cancellationToken);

        if (group == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Group not found or code is invalid."));
        }
        if (group.CodeExpirationTime < DateTime.UtcNow)
        {
            return Results.BadRequest(ApiResponse<string>.Fail("The code has expired."));
        }
        if (await dbContext.GroupUsers.AnyAsync(gu => gu.GroupId == group.Id && gu.UserId == userId, cancellationToken))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("You are already a member of this group."));
        }

        var groupUser = new GroupUser
        {
            GroupId = group.Id,
            UserId = userId,
            IsAdmin = false, // Default to non-admin
            AcceptanceStatus = AcceptanceStatus.Pending
        };

        await dbContext.GroupUsers.AddAsync(groupUser, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ApiResponse<string>.Ok("Successfully joined the group. Awaiting admin approval."));
    }

    public record JoinGroupRequest
    {
        [Required]
        [MaxLength(5)]
        public string GroupCode { get; set; }
    }
}