using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Polls;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Polls;

public class PostPoll : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/polls", Handle)
            .WithName("PostPoll")
            .WithDescription("Creates a new poll within a group by a member")
            .WithTags("Polls")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] PollRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostPoll> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to create poll. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));

        if (group.GroupUsers.All(gu => gu.UserId != userId))
            return Results.Forbid();
        
        if (string.IsNullOrWhiteSpace(request.Question))
            return Results.BadRequest(ApiResponse<string>.Fail("Question is required.", traceId));

        var poll = new Poll
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            CreatedByUserId = userId,
            Question = request.Question,
            CreatedAt = DateTime.UtcNow
        };

        poll.Options = request.Options.Select(o => new PollOption
        {
            Id = Guid.NewGuid().ToString(),
            PollId = poll.Id,
            Text = o.Text
        }).ToList();
        
        dbContext.Polls.Add(poll);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} added new poll {PollId} in group {GroupId}. TraceId: {TraceId}",
            userId, poll.Id, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Poll created successfully.", poll.Id, traceId));
    }
}