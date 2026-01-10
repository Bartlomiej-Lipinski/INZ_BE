using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Comments.Dtos;
using Mates.Features.Recommendations.Dtos;
using Mates.Features.Storage.Dtos;
using Mates.Features.Users.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mates.Features.Recommendations;

public class GetGroupRecommendations : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/recommendations", Handle)
            .WithName("GetGroupRecommendations")
            .WithDescription("Retrieves all recommendations for a specific group")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupRecommendations> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        logger.LogInformation("Fetching recommendations for group {GroupId}. TraceId: {TraceId}", groupId, traceId);

        var recommendations = await dbContext.Recommendations
            .AsNoTracking()
            .Where(r => r.GroupId == groupId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        if (recommendations.Count == 0)
        {
            logger.LogInformation(
                "[GetGroupRecommendations] No recommendations found for group {GroupId}. TraceId: {TraceId}", groupId,
                traceId);
            return Results.Ok(ApiResponse<List<RecommendationResponseDto>>
                .Ok([], "No recommendations found.", traceId));
        }

        var recommendationIds = recommendations.Select(r => r.Id).ToList();

        var comments = await dbContext.Comments
            .AsNoTracking()
            .Where(c => recommendationIds.Contains(c.TargetId))
            .Include(c => c.User)
            .ToListAsync(cancellationToken);

        var reactions = await dbContext.Reactions
            .AsNoTracking()
            .Where(r => recommendationIds.Contains(r.TargetId))
            .Include(r => r.User)
            .ToListAsync(cancellationToken);

        var commentsByRecommendation = comments
            .GroupBy(c => c.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var reactionsByRecommendation = reactions
            .GroupBy(r => r.TargetId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var userIds = recommendations.Select(f => f.UserId)
            .Concat(comments.Select(c => c.UserId))
            .Concat(reactions.Select(r => r.UserId))
            .Distinct()
            .ToList();

        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);
        
        var filesByRecommendation = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f =>
                f.GroupId == groupId &&
                f.EntityType == EntityType.Recommendation &&
                recommendationIds.Contains(f.EntityId!))
            .GroupBy(f => f.EntityId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken);
        
        var response = recommendations.Select(r => new RecommendationResponseDto
        {
            Id = r.Id,
            Title = r.Title,
            Content = r.Content,
            Category = r.Category,
            LinkUrl = r.LinkUrl,
            CreatedAt = r.CreatedAt.ToLocalTime(),
            User = new UserResponseDto
            {
                Id = r.UserId,
                Name = r.User.Name,
                Surname = r.User.Surname,
                Username = r.User.UserName,
                ProfilePicture = profilePictures.TryGetValue(r.UserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Id = photo.Id,
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null
            },
            StoredFileId = filesByRecommendation.TryGetValue(r.Id, out var files) ? files.First().Id : null,
            Comments = commentsByRecommendation.TryGetValue(r.Id, out var recComments)
                ? recComments.Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    User = new UserResponseDto
                    {
                        Id = c.UserId,
                        Name = c.User.Name,
                        Surname = c.User.Surname,
                        Username = c.User.UserName,
                        ProfilePicture = profilePictures.TryGetValue(c.UserId, out var commentsPhoto)
                            ? new ProfilePictureResponseDto
                            {
                                Id = commentsPhoto.Id,
                                Url = commentsPhoto.Url,
                                FileName = commentsPhoto.FileName,
                                ContentType = commentsPhoto.ContentType,
                                Size = commentsPhoto.Size
                            }
                            : null
                    },
                    Content = c.Content,
                    CreatedAt = c.CreatedAt.ToLocalTime()
                }).ToList()
                : [],
            Reactions = reactionsByRecommendation.TryGetValue(r.Id, out var recReactions)
                ? recReactions.Select(re => new UserResponseDto
                {
                    Id = re.UserId,
                    Name = re.User.Name,
                    Surname = re.User.Surname,
                    Username = re.User.UserName,
                    ProfilePicture = profilePictures.TryGetValue(re.UserId, out var reactionsPhoto)
                        ? new ProfilePictureResponseDto
                        {
                            Id = reactionsPhoto.Id,
                            Url = reactionsPhoto.Url,
                            FileName = reactionsPhoto.FileName,
                            ContentType = reactionsPhoto.ContentType,
                            Size = reactionsPhoto.Size
                        }
                        : null
                }).ToList()
                : []
        }).ToList();

        if (response.Count == 0)
            return Results.Ok(ApiResponse<List<RecommendationResponseDto>>
                .Ok(response, "No recommendations found for this group.", traceId));

        logger.LogInformation("Retrieved {Count} recommendations for group {GroupId}. TraceId: {TraceId}",
            response.Count, groupId, traceId);
        return Results.Ok(ApiResponse<List<RecommendationResponseDto>>
            .Ok(response, "Group recommendations retrieved successfully.", traceId));
    }
}