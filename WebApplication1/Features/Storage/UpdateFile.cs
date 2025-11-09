using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Storage;

public class UpdateFile : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/files/{id}", Handle)
            .WithName("UpdateFile")
            .WithDescription("Replace existing file")
            .WithTags("Storage")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>()
            .Accepts<IFormFile>("multipart/form-data");
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string id,
        IFormFile file,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateFile> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        var record = await dbContext.StoredFiles.FindAsync([id], cancellationToken);
        if (record == null)
        {
            logger.LogInformation("File {Id} not found for update. TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("File not found.", traceId));
        }

        if (file.Length == 0)
            return Results.BadRequest(ApiResponse<string>.Fail("No file uploaded.", traceId));

        await storage.DeleteFileAsync(record.Url, cancellationToken);

        string newUrl;
        await using (var stream = file.OpenReadStream())
        {
            newUrl = await storage.SaveFileAsync(stream, file.FileName, file.ContentType, cancellationToken);
        }

        record.FileName = file.FileName;
        record.ContentType = file.ContentType;
        record.Size = file.Length;
        record.Url = newUrl;
        record.UploadedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated file {FileId}. TraceId: {TraceId}", userId, id, traceId);

        var dto = new StoredFileResponseDto
        {
            Id = record.Id,
            FileName = record.FileName,
            ContentType = record.ContentType,
            Size = record.Size,
            Url = record.Url,
            EntityType = record.EntityType.ToString(),
            EntityId = record.EntityId!,
            UploadedAt = record.UploadedAt
        };

        return Results.Ok(ApiResponse<StoredFileResponseDto>.Ok(dto, "File updated.", traceId));
    }
}