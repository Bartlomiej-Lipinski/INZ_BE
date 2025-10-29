using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Storage
{
    public class PostFile : IEndpoint
    {
        public void RegisterEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/entities/{entityType}/{entityId}/files", Handle)
                .WithName("PostFile")
                .WithDescription("Upload file and link it to an entity")
                .WithTags("Storage")
                .RequireAuthorization()
                .DisableAntiforgery()
                .Accepts<IFormFile>("multipart/form-data")
                .WithOpenApi();
        }

        public static async Task<IResult> Handle(
            [FromRoute] string entityType,
            [FromRoute] string entityId,
            IFormFile file,
            AppDbContext dbContext,
            IStorageService storage,
            ClaimsPrincipal currentUser,
            HttpContext httpContext,
            ILogger<PostFile> logger,
            CancellationToken cancellationToken)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? currentUser.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                logger.LogWarning("Unauthorized upload attempt. TraceId: {TraceId}", traceId);
                return Results.Unauthorized();
            }

            if (file == null || file.Length == 0)
                return Results.BadRequest(ApiResponse<string>.Fail("No file uploaded.", traceId));

            string url;
            using (var stream = file.OpenReadStream())
            {
                url = await storage.SaveFileAsync(stream, file.FileName, file.ContentType ?? "application/octet-stream", cancellationToken);
            }

            var record = new StoredFile
            {
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                Size = file.Length,
                Url = url,
                EntityType = entityType,
                EntityId = entityId,
                UploadedBy = currentUserId,
                UploadedAt = DateTime.UtcNow
            };

            await dbContext.StoredFiles.AddAsync(record, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} uploaded file {FileId} for {EntityType}/{EntityId}. TraceId: {TraceId}",
                currentUserId, record.Id, entityType, entityId, traceId);

            var dto = new StoredFileResponseDto
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

            return Results.Ok(ApiResponse<StoredFileResponseDto>.Ok(dto, "File uploaded.", traceId));
        }

        public record StoredFileResponseDto
        {
            public string Id { get; init; } = null!;
            public string FileName { get; init; } = null!;
            public string ContentType { get; init; } = null!;
            public long Size { get; init; }
            public string Url { get; init; } = null!;
            public string EntityType { get; init; } = null!;
            public string EntityId { get; init; } = null!;
            public DateTime UploadedAt { get; init; }
        }
    }
}