using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Storage.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Storage;
using Mates.Infrastructure.Data.Enums;
using Mates.Infrastructure.Service;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Storage;

public class UploadUserProfilePhoto : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/users/profile/photo", Handle)
            .WithName("UploadUserProfilePhoto")
            .WithDescription("Uploads and updates the user's profile photo (scaled and validated).")
            .WithTags("Users")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data");
    }
    
    public static async Task<IResult> Handle(
        IFormFile file,
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        IStorageService storage,
        HttpContext httpContext,
        ILogger<UploadUserProfilePhoto> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        if (file == null || file.Length == 0)
        {
            logger.LogWarning("User {UserId} attempted to upload an empty file. TraceId: {TraceId}", userId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("No file uploaded.", traceId));
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            logger.LogWarning("User {UserId} tried to upload unsupported file type {ContentType}. TraceId: {TraceId}",
                userId, file.ContentType, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Unsupported image format. Use JPG, PNG, or WEBP.", traceId));
        }

        logger.LogInformation("User {UserId} is uploading a new profile photo. TraceId: {TraceId}", userId, traceId);

        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User {UserId} not found during photo upload. TraceId: {TraceId}", userId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("User not found.", traceId));
        }
        
        var record = await dbContext.StoredFiles
            .FirstOrDefaultAsync(f => f.UploadedById == userId && f.EntityType == EntityType.User, cancellationToken);

        if (record != null)
        {
            try
            {
                await storage.DeleteFileAsync(record.Url, cancellationToken);
                dbContext.StoredFiles.Remove(record);
                logger.LogInformation("Deleted old profile photo for user {UserId}. TraceId: {TraceId}", userId, traceId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old profile photo for user {UserId}. TraceId: {TraceId}", userId, traceId);
            }
        }

        string url;
        await using (var stream = file.OpenReadStream())
        {
            url = await storage.SaveFileAsync(stream, file.FileName, file.ContentType, cancellationToken);
        }

        var storedFile = new StoredFile
        {
            Id = Guid.NewGuid().ToString(),
            UploadedById = userId!,
            EntityType = EntityType.User,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            Url = url,
            UploadedAt = DateTime.UtcNow
        };
        
        await dbContext.StoredFiles.AddAsync(storedFile, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated profile photo successfully. TraceId: {TraceId}", userId, traceId);

        var dto = new ProfilePictureResponseDto
        {
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            Size = file.Length,
            Url = url,
        };

        return Results.Ok(ApiResponse<ProfilePictureResponseDto>.Ok(dto, "Profile photo updated successfully.", traceId));
    }
}