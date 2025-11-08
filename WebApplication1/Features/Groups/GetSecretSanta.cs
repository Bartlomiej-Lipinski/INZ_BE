using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GetSecretSanta : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/secret-santa", Handle)
            .WithName("GetSecretSanta")
            .WithDescription("Assigns Secret Santa pairs within a group")
            .WithTags("Groups")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetSecretSanta> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers.FirstOrDefault(gu => gu.UserId == userId);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to retrieve event in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        var groupUsers = await dbContext.GroupUsers
            .Include(gu => gu.User)
            .Where(gu => gu.GroupId == groupId && gu.AcceptanceStatus == AcceptanceStatus.Accepted)
            .ToListAsync(cancellationToken);

        if (groupUsers.Count < 2)
        {
            logger.LogWarning("Not enough users in group {GroupId} for Secret Santa. TraceId: {TraceId}",
                groupId, traceId);
            return Results.BadRequest(
                ApiResponse<string>.Fail("At least two users are required in the group for Secret Santa.", traceId));
        }

        var userInfos = groupUsers
            .Select(gu =>
            {
                var fullName = $"{gu.User?.Name ?? string.Empty} {gu.User?.Surname ?? string.Empty}".Trim();
                if (string.IsNullOrEmpty(fullName))
                    fullName = gu.UserId;
                return new { gu.UserId, FullName = fullName };
            })
            .ToList();

        var shuffled = userInfos.OrderBy(_ => Random.Shared.Next()).ToList();

        var receivers = shuffled.Skip(1).Append(shuffled.First()).ToList();

        var pairs = new List<SecretSantaResponseDto>();
        for (var i = 0; i < shuffled.Count; i++)
            pairs.Add(new SecretSantaResponseDto
            { 
                Giver = shuffled[i].FullName, 
                Receiver = receivers[i].FullName
                
            }
        );

        logger.LogInformation("Secret Santa pairs assigned for GroupId: {GroupId}. TraceId: {TraceId}",
            groupId, traceId);

        return Results.Ok(
            ApiResponse<List<SecretSantaResponseDto>>.Ok(pairs, "Secret Santa pairs assigned successfully",
                traceId));
    }
}