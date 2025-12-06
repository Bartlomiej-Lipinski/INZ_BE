using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Challenges.Dtos;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Enums;
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
        logger.LogInformation("Fetching all challenges for group {GroupId}, traceId: {TraceId}", groupId, traceId);
        
        var challenges = await dbContext.Challenges
            .Include(c => c.User)
            .Include(c => c.Participants)
            .ThenInclude(cp => cp.User)
            .Where(c => c.GroupId == groupId)
            .OrderBy(c => c.StartDate)
            .ToListAsync(cancellationToken);
        
        var now = DateTime.UtcNow;
        var challengesUpdated = false;

        foreach (var challenge in challenges.Where(challenge => !challenge.IsCompleted && now >= challenge.EndDate))
        {
            challenge.IsCompleted = true;
            challengesUpdated = true;

            foreach (var participant in challenge.Participants)
            {
                participant.Completed = participant.TotalProgress >= challenge.GoalValue;
                participant.CompletedAt ??= now;
            }

            logger.LogInformation("Challenge {ChallengeId} automatically completed, traceId: {TraceId}",
                challenge.Id, traceId);
        }
        
        if (challengesUpdated) 
            await dbContext.SaveChangesAsync(cancellationToken);
        
        var userIds = challenges.Select(c => c.UserId).Distinct().ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);
        
        var response = challenges.Select(c => new ChallengeResponseDto
        {
            Id = c.Id,
            User = new UserResponseDto
            {
                Id = c.UserId,
                Name = c.User.Name,
                Surname = c.User.Surname,
                Username = c.User.UserName,
                ProfilePicture = profilePictures.TryGetValue(c.UserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null 
            },
            Name = c.Name,
            Description = c.Description,
            StartDate = c.StartDate.ToLocalTime(),
            EndDate = c.EndDate.ToLocalTime(),
            GoalUnit = c.GoalUnit,
            GoalValue = c.GoalValue,
            IsCompleted = c.IsCompleted,
            Participants = c.Participants.Select(p => new ChallengeParticipantResponseDto
            {
                User = new UserResponseDto
                {
                    Id = p.UserId,
                    Name = p.User.Name,
                    Surname = p.User.Surname,
                    Username = p.User.UserName,
                    ProfilePicture = profilePictures.TryGetValue(p.UserId, out var participantPhoto)
                        ? new ProfilePictureResponseDto
                        {
                            Url = participantPhoto.Url,
                            FileName = participantPhoto.FileName,
                            ContentType = participantPhoto.ContentType,
                            Size = participantPhoto.Size
                        }
                        : null 
                },
                JoinedAt = p.JoinedAt,
                CompletedAt = p.CompletedAt
            }).ToList()
        }).ToList();
        
        if (response.Count == 0)
        {
            logger.LogInformation("No challenges found for group {GroupId}, traceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<List<ChallengeResponseDto>>
                .Ok(response, "No challenges found for this group.", traceId));
        }
        
        logger.LogInformation("Retrieved {Count} challenges for group {GroupId}, traceId: {TraceId}",
            response.Count, groupId, traceId);
        return Results.Ok(ApiResponse<List<ChallengeResponseDto>>
            .Ok(response, "Group challenges retrieved successfully.", traceId));
    }
}