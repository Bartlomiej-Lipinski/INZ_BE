using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GetGroupUsers : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/users", Handle)
            .WithName("GetGroupUsers")
            .WithDescription("Retrieves all users for a specific group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupUsers> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to get group users. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        var isCurrentUserMemberOfGroup = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == userId, cancellationToken);
        if (!isCurrentUserMemberOfGroup)
        {
            logger.LogWarning("User {UserId} tried to get users for group {GroupId} without membership. TraceId: {TraceId}", 
                userId, groupId, traceId);
            return Results.Forbid();
        }

        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .ThenInclude(gu => gu.User)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));

        var isMember = group.GroupUsers.Any(gu => gu.UserId == userId);
        if (!isMember)
        {
            logger.LogWarning("User {UserId} tried to get users for group {GroupId} without membership. TraceId: {TraceId}", 
                userId, groupId, traceId);
            return Results.Forbid();
        }

        var users = group.GroupUsers
            .Select(gu => new UserResponseDto
            {
                Id = gu.User.Id,
                UserName = gu.User.UserName,
                Email = gu.User.Email
            })
            .ToList();

        return Results.Ok(ApiResponse<IEnumerable<UserResponseDto>>.Ok(users, null, traceId));
    }
}