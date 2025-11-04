using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;

namespace WebApplication1.Features.Groups;

public class SecretSanta : IEndpoint
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
        ILogger<SecretSanta> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogInformation("Secret Santa assignment requested for GroupId: {GroupId}. TraceId: {TraceId}",
            groupId, traceId);

        var groupUsers = await context.GroupUsers
            .Include(gu => gu.User)
            .Where(gu => gu.GroupId == groupId)
            .ToListAsync(cancellationToken);

        if (groupUsers.Count < 2)
        {
            logger.LogWarning("Not enough users in group {GroupId} for Secret Santa. TraceId: {TraceId}",
                groupId, traceId);
            return Results.BadRequest(new { Message = "Not enough users in the group for Secret Santa." });
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

        var getGroup = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (getGroup == null)
        {
            logger.LogWarning("Group not found {GroupId} for Secret Santa. TraceId: {TraceId}",
                groupId, traceId);
            return Results.NotFound(new { Message = "Group not found." });
        }

        if (getGroup.SecretSantaPairs != null) getGroup.SecretSantaPairs = null;

        getGroup.SecretSantaPairs = pairs;
        await context.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { Message = "Secret Santa pairs assigned successfully.", Pairs = pairs });
    }

    public record SecretSantaPairDto(string Giver, string Receiver);
}