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
            .ThenInclude(p => p.ProgressEntries).Include(challenge => challenge.Participants)
            .ThenInclude(cp => cp.User)
            .FirstOrDefaultAsync(e => e.Id == challengeId && e.GroupId == groupId, cancellationToken);
        
        if (challenge == null)
        {
            logger.LogWarning("Challenge {ChallengeId} not found in group {GroupId}, traceId: {TraceId}",
                challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge not found.", traceId));
        }
        
        if (!challenge.IsCompleted && DateTime.UtcNow >= challenge.EndDate)
        {
            challenge.IsCompleted = true;
            foreach (var participant in challenge.Participants)
            {
                participant.Completed = participant.TotalProgress >= challenge.GoalValue;
                participant.CompletedAt ??= DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Challenge {ChallengeId} automatically completed, traceId: {TraceId}", challengeId, traceId);
        }

        var userIds = challenge.Participants.Select(c => c.UserId)
            .Append(challenge.UserId).Distinct().ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);
        
        var response = new ChallengeResponseDto
        {
            Id = challengeId,
            User = new UserResponseDto
            {
                Id = challenge.UserId,
                Name = challenge.User.Name,
                Surname = challenge.User.Surname,
                Username = challenge.User.UserName,
                ProfilePicture = profilePictures.TryGetValue(challenge.UserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null  
            },
            Name = challenge.Name,
            Description = challenge.Description,
            StartDate = challenge.StartDate.ToLocalTime(),
            EndDate = challenge.EndDate.ToLocalTime(),
            GoalUnit = challenge.GoalUnit,
            GoalValue = challenge.GoalValue,
            IsCompleted = challenge.IsCompleted,
            Participants = challenge.Participants.Select(p => new ChallengeParticipantResponseDto
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
                CompletedAt = p.CompletedAt,
                ProgressEntries = p.ProgressEntries.OrderBy(p => p.Date)
                    .Select(p => new ChallengeProgressResponseDto 
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