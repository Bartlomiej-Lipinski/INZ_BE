using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Storage
{
    public class UpdateFile : IEndpoint
    {
        public void RegisterEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/files/{id}", Handle)
                .WithName("UpdateFile")
                .WithDescription("Replace existing file")
                .WithTags("Storage")
                .RequireAuthorization()
                .Accepts<IFormFile>("multipart/form-data")
                .WithOpenApi();
        }

        public static async Task<IResult> Handle(
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
            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? currentUser.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                logger.LogWarning("Unauthorized update attempt. TraceId: {TraceId}", traceId);
                return Results.Unauthorized();
            }

            var record = await dbContext.StoredFiles.FindAsync(new object[] { id }, cancellationToken);
            if (record == null)
            {
                logger.LogInformation("File {Id} not found for update. TraceId: {TraceId}", id, traceId);
                return Results.NotFound(ApiResponse<string>.Fail("File not found.", traceId));
            }

            if (file == null || file.Length == 0)
                return Results.BadRequest(ApiResponse<string>.Fail("No file uploaded.", traceId));

            // remove old physical file
            await storage.DeleteFileAsync(record.Url, cancellationToken);

            // save new file
            string newUrl;
            using (var stream = file.OpenReadStream())
            {
                newUrl = await storage.SaveFileAsync(stream, file.FileName, file.ContentType ?? "application/octet-stream", cancellationToken);
            }

            // update metadata
            record.FileName = file.FileName;
            record.ContentType = file.ContentType ?? "application/octet-stream";
            record.Size = file.Length;
            record.Url = newUrl;
            record.UploadedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} updated file {FileId}. TraceId: {TraceId}", currentUserId, id, traceId);

            var dto = new PostFile.StoredFileResponseDto
            {
                Id = record.Id,
                FileName = record.FileName,
                ContentType = record.ContentType,
                Size = record.Size,
                Url = record.Url,
                EntityType = record.EntityType,
                EntityId = record.EntityId,
                UploadedAt = record.UploadedAt
            };

            return Results.Ok(ApiResponse<PostFile.StoredFileResponseDto>.Ok(dto, "File updated.", traceId));
        }
    }
}