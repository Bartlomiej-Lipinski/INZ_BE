using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Comments;

public class GetCommentsForTarget : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/comments/{targetId}", Handle)
            .WithName("GetCommentsByTarget")
            .WithDescription("Retrieves comments for a target")
            .WithTags("Comments")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string targetId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetCommentsForTarget> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        logger.LogInformation("User {UserId} fetching comments for target {TargetId} in group {GroupId}. TraceId: {TraceId}",
            userId, targetId, groupId, traceId);

        var comments = await dbContext.Comments
            .AsNoTracking()
            .Where(c => c.TargetId == targetId)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        if (comments.Count == 0)
        {
            return Results.Ok(ApiResponse<List<CommentResponseDto>>
                .Ok([], "No comments found for this target.", traceId));
        }
        
        var userIds = comments.Select(c => c.UserId).Distinct().ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);
        
        var result = comments.Select(c => new CommentResponseDto
            {
                Id = c.Id,
                Content = c.Content,
                UserName = c.User.UserName,
                CreatedAt = c.CreatedAt.ToLocalTime(),
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
                }
            })
            .ToList();
        
        if (comments.Count == 0)
        {
            logger.LogInformation("No comments found for target {TargetId}. TraceId: {TraceId}", targetId, traceId);
            return Results.Ok(ApiResponse<List<CommentResponseDto>>
                .Ok(result, "No comments found for this target.", traceId));
        }

        logger.LogInformation("Retrieved {Count} comments for target {TargetId}. TraceId: {TraceId}", 
            comments.Count, targetId, traceId);
        return Results.Ok(ApiResponse<List<CommentResponseDto>>
            .Ok(result, "Comments retrieved successfully.", traceId));
    }
}