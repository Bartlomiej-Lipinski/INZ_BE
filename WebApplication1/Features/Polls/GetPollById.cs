using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Enums;
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
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == pollId && p.GroupId == groupId, cancellationToken);

        if (poll == null)
        {
            logger.LogWarning("Poll {PollId} not found in group {GroupId}. TraceId: {TraceId}", pollId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Poll not found.", traceId));
        }
        
        var userIds = poll.Options.SelectMany(o => o.VotedUsers).Select(v => v.Id)
            .Append(poll.CreatedByUserId).Distinct().ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);

        var response = new PollResponseDto
        {
            CreatedByUser = new UserResponseDto
            {
                Id = poll.CreatedByUserId,
                Name = poll.CreatedByUser.Name,
                Surname = poll.CreatedByUser.Surname,
                Username = poll.CreatedByUser.UserName,
                ProfilePicture = profilePictures.TryGetValue(poll.CreatedByUserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null 
            },
            Question = poll.Question,
            CreatedAt = poll.CreatedAt.ToLocalTime(),
            Options = poll.Options.Select(o => new PollOptionDto
            {
                Text = o.Text,
                VotedUsers = o.VotedUsers.Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Surname = u.Surname,
                    Username = u.UserName,
                    ProfilePicture = profilePictures.TryGetValue(u.Id, out var votersPhoto)
                        ? new ProfilePictureResponseDto
                        {
                            Url = votersPhoto.Url,
                            FileName = votersPhoto.FileName,
                            ContentType = votersPhoto.ContentType,
                            Size = votersPhoto.Size
                        }
                        : null 
                }).ToList()
            }).ToList()
        };
        
        logger.LogInformation("Poll {PollId} retrieved successfully. TraceId: {TraceId}", pollId, traceId);
        return Results.Ok(ApiResponse<PollResponseDto>.Ok(response, "Poll retrieved successfully.", traceId));
    }
}