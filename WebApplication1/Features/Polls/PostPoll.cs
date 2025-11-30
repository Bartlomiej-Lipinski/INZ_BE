using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Data.Entities.Polls;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

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
            .AddEndpointFilter<GroupMembershipFilter>();
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
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} creating poll in group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);
        
        if (string.IsNullOrWhiteSpace(request.Question))
            return Results.BadRequest(ApiResponse<string>.Fail("Question is required.", traceId));
        
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var poll = new Poll
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            EntityType = EntityType.Poll,
            CreatedByUserId = userId!,
            Question = request.Question,
            CreatedAt = DateTime.UtcNow
        };

        poll.Options = request.Options.Select(o => new PollOption
        {
            Id = Guid.NewGuid().ToString(),
            PollId = poll.Id,
            Text = o.Text
        }).ToList();
        
        var feedItem = new GroupFeedItem
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId!,
            Type = FeedItemType.Poll,
            EntityId = poll.Id,
            StoredFileId = null,
            Title = request.Question,
            Description = null,
            CreatedAt = DateTime.UtcNow
        };
        
        dbContext.Polls.Add(poll);
        dbContext.GroupFeedItems.Add(feedItem);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} added new poll {PollId} in group {GroupId}. TraceId: {TraceId}",
            userId, poll.Id, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Poll created successfully.", poll.Id, traceId));
    }
}