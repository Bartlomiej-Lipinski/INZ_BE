using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Polls.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Polls;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Features.Users.Dtos;
using Mates.Shared.Extensions;

namespace Mates.Features.Polls;

public class UpdatePoll : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/polls/{pollId}", Handle)
            .WithName("UpdatePoll")
            .WithDescription("Updates an existing poll in a group")
            .WithTags("Polls")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string pollId,
        [FromBody] PollRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdatePoll> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} attempting to update poll {PollId} in group {GroupId}. TraceId: {TraceId}",
            userId, pollId, groupId, traceId);
        
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingPoll = await dbContext.Polls
            .Include(p => p.Options).ThenInclude(pollOption => pollOption.VotedUsers)
            .SingleOrDefaultAsync(p => p.Id == pollId && p.GroupId == groupId, cancellationToken);
        
        if (existingPoll == null)
        {
            logger.LogWarning("Poll {PollId} not found in group {GroupId}. TraceId: {TraceId}", pollId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Poll not found.", traceId));
        }

        if (existingPoll.CreatedByUserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to update poll {PollId} not created by them. TraceId: {TraceId}", 
                userId, pollId, traceId);
            return Results.Forbid();
        }
        
        if (!string.IsNullOrWhiteSpace(request.Question))
            existingPoll.Question = request.Question;

        if (request.Options.Count != 0)
        {
            var existingOptions = existingPoll.Options.ToDictionary(o => o.Id);
            
            foreach (var optionDto in request.Options)
            {
                if (!string.IsNullOrWhiteSpace(optionDto.Id) 
                    && existingOptions.TryGetValue(optionDto.Id, out var existingOption))
                {
                    existingOption.Text = optionDto.Text;
                    existingOptions.Remove(optionDto.Id);
                }
                else
                {
                    var newOption = new PollOption
                    {
                        Id = Guid.NewGuid().ToString(),
                        PollId = existingPoll.Id,
                        Text = optionDto.Text
                    };
                    await dbContext.PollOptions.AddAsync(newOption, cancellationToken);
                }
            }
            foreach (var toRemove in existingOptions.Values)
            {
                dbContext.PollOptions.Remove(toRemove);
            }
        }
        
        var feedItem = await dbContext.GroupFeedItems
            .SingleOrDefaultAsync(f => f.EntityId == pollId && f.GroupId == groupId, cancellationToken);
        if (feedItem != null)
        {
            feedItem.Title = existingPoll.Question;
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(existingPoll)
            .Collection(p => p.Options)
            .Query()
            .Include(o => o.VotedUsers)
            .LoadAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = new PollResponseDto
        {
            CreatedByUserId = existingPoll.CreatedByUserId,
            Question = existingPoll.Question,
            CreatedAt = existingPoll.CreatedAt,
            Options = existingPoll.Options.Select(o => new PollOptionDto
            {
                Text = o.Text,
                VotedUsersIds = o.VotedUsers.Select(u => u.Id).ToList()
            }).ToList()
        };
        
        logger.LogInformation("Poll {PollId} updated successfully by user {UserId}. TraceId: {TraceId}", 
            pollId, userId, traceId);
        return Results.Ok(ApiResponse<PollResponseDto>.Ok(response, "Poll updated successfully.", traceId));
    }
}