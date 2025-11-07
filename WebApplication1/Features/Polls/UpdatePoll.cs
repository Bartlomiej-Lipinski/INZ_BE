using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Entities.Polls;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Polls;

public class UpdatePoll : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/polls/{pollId}", Handle)
            .WithName("UpdatePoll")
            .WithDescription("Updates an existing poll in a group")
            .WithTags("Polls")
            .RequireAuthorization();
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
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to update poll. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to update poll in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }

        var existingPoll = await dbContext.Polls
            .Include(p => p.Options).ThenInclude(pollOption => pollOption.VotedUsers)
            .FirstOrDefaultAsync(p => p.Id == pollId && p.GroupId == groupId, cancellationToken);
        
        if (existingPoll == null)
        {
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
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(existingPoll)
            .Collection(p => p.Options)
            .Query()
            .Include(o => o.VotedUsers)
            .LoadAsync(cancellationToken);

        var response = new PollResponseDto
        {
            CreatedByUserId = existingPoll.CreatedByUserId,
            Question = existingPoll.Question,
            CreatedAt = existingPoll.CreatedAt,
            Options = existingPoll.Options.Select(o => new PollOptionDto
            {
                Text = o.Text,
                VotedUsers = o.VotedUsers.Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.UserName
                }).ToList()
            }).ToList()
        };
        
        return Results.Ok(ApiResponse<PollResponseDto>.Ok(response, "Poll updated successfully.", traceId));
    }
}