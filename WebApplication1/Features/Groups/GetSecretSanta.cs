using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GetSecretSanta : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/secret-santa", Handle)
            .WithName("SecretSanta")
            .WithDescription("Assigns Secret Santa pairs within a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext context,
        HttpContext httpContext,
        ILogger<GetSecretSanta> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? httpContext.User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to access Secret Santa. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var getGroup = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (getGroup == null)
        {
            logger.LogWarning("Group not found {GroupId} for Secret Santa. TraceId: {TraceId}",
                groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var isUserInGroup = await context.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == currentUserId, cancellationToken);

        if (!isUserInGroup)
        {
            logger.LogWarning(
                "User {UserId} attempted to access Secret Santa for group {GroupId} without membership. TraceId: {TraceId}",
                currentUserId, groupId, traceId);
            return Results.Forbid();
        }

        logger.LogInformation("Secret Santa assignment requested for GroupId: {GroupId}. TraceId: {TraceId}",
            groupId, traceId);

        var groupUsers = await context.GroupUsers
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

        var pairs = new List<SecretSantaPairDto>();
        for (var i = 0; i < shuffled.Count; i++)
            pairs.Add(new SecretSantaPairDto(shuffled[i].FullName, receivers[i].FullName));

        logger.LogInformation("Secret Santa pairs assigned for GroupId: {GroupId}. TraceId: {TraceId}",
            groupId, traceId);

        return Results.Ok(
            ApiResponse<List<SecretSantaPairDto>>.Ok(pairs, "Secret Santa pairs assigned successfully", traceId));
    }

    public record SecretSantaPairDto(string Giver, string Receiver);
}