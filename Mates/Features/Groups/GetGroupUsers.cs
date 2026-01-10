using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Users.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Groups;

public class GetGroupUsers : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/users", Handle)
            .WithName("GetGroupUsers")
            .WithDescription("Retrieves all users for a specific group")
            .WithTags("Groups")
            .RequireAuthorization();
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
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} requested users for group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);
        
        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .ThenInclude(gu => gu.User)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }
        
        var isCurrentUserMemberOfGroup = await dbContext.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId 
                            && gu.UserId == userId
                            && gu.AcceptanceStatus == AcceptanceStatus.Accepted, cancellationToken);
        
        if (!isCurrentUserMemberOfGroup)
        {
            logger.LogWarning("User {UserId} tried to get users for group {GroupId} without membership. TraceId: {TraceId}", 
                userId, groupId, traceId);
            return Results.Forbid();
        }

        var users = group.GroupUsers
            .Select(gu => new UserResponseDto
            {
                Id = gu.User.Id,
                Username = gu.User.UserName,
                Email = gu.User.Email,
                Name = gu.User.Name,
                Surname = gu.User.Surname,
                BirthDate = gu.User.BirthDate,
                Status = gu.User.Status,
                Description = gu.User.Description
            })
            .ToList();
        
        if (users.Count == 0)
            return Results.Ok(ApiResponse<List<UserResponseDto>>
                .Ok(users, "No users found for this group.", traceId));

        logger.LogInformation("Retrieved {UserCount} users for group {GroupId}. TraceId: {TraceId}",
            users.Count, groupId, traceId);
        return Results.Ok(ApiResponse<IEnumerable<UserResponseDto>>.Ok(users, null, traceId));
    }
}