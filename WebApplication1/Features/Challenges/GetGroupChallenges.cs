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

public class GetGroupChallenges : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/challenges", Handle)
            .WithName("GetGroupChallenges")
            .WithDescription("Retrieves all challenges for a specific group")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupChallenges> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        var challenges = await dbContext.Challenges
            .AsNoTracking()
            .Include(c => c.Participants)
            .Include(c => c.Stages)
            .Where(c => c.GroupId == groupId)
            .OrderBy(c => c.StartDate)
            .Select(c => new ChallengeResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                StartDate = c.StartDate.ToLocalTime(),
                EndDate = c.EndDate.HasValue ? c.EndDate.Value.ToLocalTime() : null,
                IsCompleted = c.IsCompleted,
                Participants = c.Participants.Select(p => new ChallengeParticipantResponseDto
                {
                    UserId = p.UserId,
                    Points = p.Points
                }).ToList()
            }).ToListAsync(cancellationToken);
        
        if (challenges.Count == 0)
            return Results.Ok(ApiResponse<List<ChallengeResponseDto>>
                .Ok(challenges, "No challenges found for this group.", traceId));
            
        return Results.Ok(ApiResponse<List<ChallengeResponseDto>>
            .Ok(challenges, "Group challenges retrieved successfully.", traceId));
    }
}