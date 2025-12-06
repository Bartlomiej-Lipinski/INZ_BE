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

public class GetGroupPolls : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/polls", Handle)
            .WithName("GetGroupPolls")
            .WithDescription("Retrieves all polls for a specific group")
            .WithTags("Polls")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupPolls> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        logger.LogInformation("Fetching polls for group {GroupId}. TraceId: {TraceId}", groupId, traceId);

        var polls = await dbContext.Polls
            .AsNoTracking()
            .Include(p => p.Options)
            .ThenInclude(o => o.VotedUsers)
            .Include(p => p.CreatedByUser)
            .Where(p => p.GroupId == groupId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        
        var userIds = polls
            .Select(p => p.CreatedByUserId)
            .Concat(polls.SelectMany(p => p.Options).SelectMany(o => o.VotedUsers).Select(v => v.Id))
            .Distinct()
            .ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);

        var response = polls.Select(p => new PollResponseDto
        {
            Id = p.Id,
            CreatedByUser = new UserResponseDto
            {
                Id = p.CreatedByUserId,
                Name = p.CreatedByUser.Name,
                Surname = p.CreatedByUser.Surname,
                Username = p.CreatedByUser.UserName,
                ProfilePicture = profilePictures.TryGetValue(p.CreatedByUserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null 
            },
            Question = p.Question,
            CreatedAt = p.CreatedAt.ToLocalTime(),
            Options = p.Options.Select(o => new PollOptionDto
            {
                Id = o.Id,
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
        })
        .ToList();
        
        if (polls.Count == 0)
        {
            logger.LogInformation("No polls found for group {GroupId}. TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<List<PollResponseDto>>.Ok(response, "No polls found for this group.",
                traceId));
        }

        logger.LogInformation("Retrieved {Count} polls for group {GroupId}. TraceId: {TraceId}", polls.Count, groupId,
            traceId);
        return Results.Ok(ApiResponse<List<PollResponseDto>>.Ok(response, "Group polls retrieved successfully.",
            traceId));
    }
}