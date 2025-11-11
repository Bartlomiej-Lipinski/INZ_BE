using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Challenges.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Challenges;

public class GetChallengeById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/challenges/{challengeId}", Handle)
            .WithName("GetChallengeById")
            .WithDescription("Retrieves a single challenge by its ID within a group")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string challengeId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetChallengeById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        logger.LogInformation("Fetching challenge {ChallengeId} for group {GroupId}, traceId: {TraceId}",
            challengeId, groupId, traceId);

        var challenge = await dbContext.Challenges
            .AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.Participants)
            .ThenInclude(p => p.ProgressEntries)
            .FirstOrDefaultAsync(e => e.Id == challengeId && e.GroupId == groupId, cancellationToken);
        
        if (challenge == null)
        {
            logger.LogWarning("Challenge {ChallengeId} not found in group {GroupId}, traceId: {TraceId}",
                challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge not found.", traceId));
        }
        
        var response = new ChallengeResponseDto
        {
            Id = challengeId,
            UserId = challenge.UserId,
            Name = challenge.Name,
            Description = challenge.Description,
            StartDate = challenge.StartDate.ToLocalTime(),
            EndDate = challenge.EndDate.ToLocalTime(),
            GoalUnit = challenge.GoalUnit,
            GoalValue = challenge.GoalValue,
            IsCompleted = challenge.IsCompleted,
            Participants = challenge.Participants.Select(p => new ChallengeParticipantResponseDto
            {
                UserId = p.UserId,
                JoinedAt = p.JoinedAt,
                CompletedAt = p.CompletedAt,
                ProgressEntries = p.ProgressEntries.Select(p => new ChallengeProgressResponseDto
                {
                    Date = p.Date,
                    Description = p.Description,
                    Value = p.Value
                }).ToList()
            }).ToList()
        };
        
        logger.LogInformation("Returning challenge {ChallengeId} response, traceId: {TraceId}", challengeId, traceId);
        return Results.Ok(ApiResponse<ChallengeResponseDto>.Ok(response, null, traceId));
    }
}