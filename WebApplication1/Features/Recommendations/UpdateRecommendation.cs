using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Recommendations.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Recommendations;

public class UpdateRecommendation : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/recommendations/{recommendationId}", Handle)
            .WithName("UpdateRecommendation")
            .WithDescription("Updates an existing recommendation if the user is the author.")
            .WithTags("Recommendations")
            .RequireAuthorization()
            .Accepts<RecommendationRequestDto>("multipart/form-data")
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string recommendationId,
        [FromForm] RecommendationRequestDto request,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateRecommendation> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} attempting to update recommendation {RecommendationId} in group {GroupId}. TraceId: {TraceId}", 
            userId, recommendationId, groupId, traceId);
        
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        var recommendation = await dbContext.Recommendations
            .SingleOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {RecommendationId} not found. TraceId: {TraceId}", recommendationId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Recommendation not found.", traceId));
        }
        
        if (recommendation.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to edit recommendation {RecommendationId} they do not own. " +
                              "TraceId: {TraceId}", userId, recommendationId, traceId);
            return Results.Forbid();
        }
        
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            logger.LogWarning("Invalid update data from user {UserId}. TraceId: {TraceId}", userId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Title and Content are required.", traceId));
        }
        
        recommendation.Title = request.Title.Trim();
        recommendation.Content = request.Content.Trim();
        recommendation.Category = request.Category?.Trim();
        recommendation.ImageUrl = request.ImageUrl;
        recommendation.LinkUrl = request.LinkUrl;
        recommendation.UpdatedAt = DateTime.UtcNow;
        
        var feedItem = await dbContext.GroupFeedItems
            .SingleOrDefaultAsync(f => f.EntityId == recommendationId && f.GroupId == groupId, cancellationToken);
        if (feedItem != null)
        {
            feedItem.Title = recommendation.Title;
            feedItem.Description = recommendation.Content;
        }
        
        string? storedFileId = null;
        if (request.File != null)
        {
            if (feedItem?.StoredFileId != null)
            {
                var oldFile = await dbContext.StoredFiles
                    .SingleOrDefaultAsync(f => f.Id == feedItem.StoredFileId, cancellationToken);

                if (oldFile != null)
                {
                    await storage.DeleteFileAsync(oldFile.Url, cancellationToken);
                    dbContext.StoredFiles.Remove(oldFile);
                }
            }
            
            string url;
            await using (var stream = request.File.OpenReadStream())
            {
                url = await storage.SaveFileAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    cancellationToken);
            }

            storedFileId = Guid.NewGuid().ToString();

            var storedFile = new StoredFile
            {
                Id = storedFileId,
                GroupId = groupId,
                UploadedById = userId,
                EntityType = EntityType.Recommendation,
                EntityId = recommendationId,
                FileName = request.File.FileName,
                ContentType = request.File.ContentType,
                Size = request.File.Length,
                Url = url,
                UploadedAt = DateTime.UtcNow
            };
            
            feedItem!.StoredFileId = storedFileId;
            dbContext.StoredFiles.Add(storedFile);
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} updated recommendation {RecommendationId}. TraceId: {TraceId}",
            userId, recommendationId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Recommendation updated successfully.", 
            recommendationId, traceId));
    }
}