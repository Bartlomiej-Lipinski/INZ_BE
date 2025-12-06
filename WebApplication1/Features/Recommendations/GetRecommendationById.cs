using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Features.Recommendations.Dtos;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Recommendations;

public class GetRecommendationById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/recommendations/{recommendationId}", Handle)
            .WithName("GetRecommendationById")
            .WithDescription("Retrieves a single recommendation by its ID")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string recommendationId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetRecommendationById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        logger.LogInformation("Fetching recommendation {RecommendationId} in group {GroupId}. TraceId: {TraceId}",
            recommendationId, groupId, traceId);
        
        if (string.IsNullOrWhiteSpace(recommendationId))
        {
            logger.LogWarning("Recommendation ID is missing. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Recommendation ID is required.", traceId));
        }
        
        var recommendation = await dbContext.Recommendations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Group)
            .FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation not found: {RecommendationId}. TraceId: {TraceId}", recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }
        
        var comments = await dbContext.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.TargetId == recommendationId && c.EntityType == EntityType.Recommendation)
            .ToListAsync(cancellationToken);

        var reactions = await dbContext.Reactions
            .AsNoTracking()
            .Where(r => r.TargetId == recommendationId && r.EntityType == EntityType.Recommendation)
            .Include(r => r.User)
            .ToListAsync(cancellationToken);
        
        var userIds = comments.Select(c => c.UserId)
            .Concat(reactions.Select(r => r.UserId))
            .Append(recommendation.UserId).Distinct().ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);

        var response = new RecommendationResponseDto
        {
            Id = recommendation.Id,
            Title = recommendation.Title,
            Content = recommendation.Content,
            Category = recommendation.Category,
            ImageUrl = recommendation.ImageUrl,
            LinkUrl = recommendation.LinkUrl,
            CreatedAt = recommendation.CreatedAt.ToLocalTime(),
            User = new UserResponseDto
            {
                Id = recommendation.UserId,
                Name = recommendation.User.Name,
                Surname = recommendation.User.Surname,
                Username = recommendation.User.UserName,
                ProfilePicture = profilePictures.TryGetValue(recommendation.UserId, out var photo)
                    ? new ProfilePictureResponseDto
                    {
                        Url = photo.Url,
                        FileName = photo.FileName,
                        ContentType = photo.ContentType,
                        Size = photo.Size
                    }
                    : null 
            },
            Comments = comments.Select(c => new CommentResponseDto
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
                            Url = commentsPhoto.Url,
                            FileName = commentsPhoto.FileName,
                            ContentType = commentsPhoto.ContentType,
                            Size = commentsPhoto.Size
                        }
                        : null 
                },
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToLocalTime()
            }).ToList(),
            Reactions = reactions.Select(r => new UserResponseDto
            {
                Id = r.UserId,
                Name = r.User.Name,
                Surname = r.User.Surname,
                Username = r.User.UserName,
                ProfilePicture = profilePictures.TryGetValue(r.UserId, out var reactionsPhoto)
                    ? new ProfilePictureResponseDto
                    {
                        Url = reactionsPhoto.Url,
                        FileName = reactionsPhoto.FileName,
                        ContentType = reactionsPhoto.ContentType,
                        Size = reactionsPhoto.Size
                    }
                    : null 
            }).ToList()
        };

        logger.LogInformation("Recommendation retrieved successfully: {RecommendationId}. TraceId: {TraceId}",
            recommendationId, traceId);
        return Results.Ok(ApiResponse<RecommendationResponseDto>.Ok(response, null, traceId));
    }
}