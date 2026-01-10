using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Storage.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Service;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Mates.Shared.Extensions;

namespace Mates.Features.Storage;

public class DeleteUserProfilePhoto : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/users/profile/photo/", Handle)
            .WithName("DeleteUserProfilePhoto")
            .WithDescription("Delete current user's profile photo")
            .WithTags("Users")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromBody] DeleteProfilePhotoDto request,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteFile> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        var record = await dbContext.StoredFiles.FindAsync(request.FileId, cancellationToken);
        if (record == null)
        {
            logger.LogInformation("File {Id} not found for delete. TraceId: {TraceId}", request.FileId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("File not found.", traceId));
        }

        if (record.UploadedById != userId)
        {
            logger.LogWarning("User {UserId} attempted to delete file {FileId} they do not own and is not admin. " +
                              "TraceId: {TraceId}", userId, request.FileId, traceId);
            return Results.Forbid();
        }

        await storage.DeleteFileAsync(record.Url, cancellationToken);
        dbContext.StoredFiles.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted file {FileId}. TraceId: {TraceId}", userId, request.FileId,
            traceId);

        return Results.Ok(ApiResponse<string>.Ok(null!, "File deleted.", traceId));
    }
}