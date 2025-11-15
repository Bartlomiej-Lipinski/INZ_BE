using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Polls;

public class GetPollById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/polls/{pollId}", Handle)
            .WithName("GetPollById")
            .WithDescription("Retrieves a single poll by its ID within a group")
            .WithTags("Polls")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string pollId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetPollById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        logger.LogInformation("Fetching poll {PollId} in group {GroupId}. TraceId: {TraceId}", pollId, groupId, traceId);

        var poll = await dbContext.Polls
            .AsNoTracking()
            .Include(p => p.Options)
            .ThenInclude(o => o.VotedUsers)
            .FirstOrDefaultAsync(p => p.Id == pollId && p.GroupId == groupId, cancellationToken);

        if (poll == null)
        {
            logger.LogWarning("Poll {PollId} not found in group {GroupId}. TraceId: {TraceId}", pollId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Poll not found.", traceId));
        }

        var response = new PollResponseDto
        {
            CreatedByUserId = poll.CreatedByUserId,
            Question = poll.Question,
            CreatedAt = poll.CreatedAt.ToLocalTime(),
            Options = poll.Options.Select(o => new PollOptionDto
            {
                Text = o.Text,
                VotedUsers = o.VotedUsers.Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.UserName
                }).ToList()
            }).ToList()
        };
        
        logger.LogInformation("Poll {PollId} retrieved successfully. TraceId: {TraceId}", pollId, traceId);
        return Results.Ok(ApiResponse<PollResponseDto>.Ok(response, "Poll retrieved successfully.", traceId));
    }
}