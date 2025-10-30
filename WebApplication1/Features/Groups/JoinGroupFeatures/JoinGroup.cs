using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups.JoinGroupFeatures;

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
        [FromBody] JoinGroupRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }
        
        if (string.IsNullOrWhiteSpace(request.GroupCode))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Group ID and Code are required.", traceId));
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g =>  g.Code == request.GroupCode, cancellationToken);

        if (group == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Group not found or code is invalid.", traceId));
        }
        if (group.CodeExpirationTime < DateTime.UtcNow)
        {
            return Results.BadRequest(ApiResponse<string>.Fail("The code has expired.", traceId));
        }
        if (await dbContext.GroupUsers.AnyAsync(gu => gu.GroupId == group.Id && gu.UserId == userId, cancellationToken))
        {
            return Results
                .BadRequest(ApiResponse<string>.Fail("You are already a member of this group.", traceId));
        }

        var groupUser = new GroupUser
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = group.Id,
            UserId = userId,
            IsAdmin = false,
            AcceptanceStatus = AcceptanceStatus.Pending
        };

        await dbContext.GroupUsers.AddAsync(groupUser, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ApiResponse<string>.Ok("Successfully joined the group. Awaiting admin approval.",
            null, traceId));
    }
}