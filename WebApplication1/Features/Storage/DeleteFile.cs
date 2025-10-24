using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Storage;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Storage
{
    public class DeleteFile : IEndpoint
    {
        public void RegisterEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/files/{id}", Handle)
                .WithName("DeleteFile")
                .WithDescription("Delete file by id")
                .WithTags("Storage")
                .RequireAuthorization()
                .WithOpenApi();
        }

        public static async Task<IResult> Handle(
            [FromRoute] string id,
            AppDbContext dbContext,
            IStorageService storage,
            ClaimsPrincipal currentUser,
            HttpContext httpContext,
            ILogger<DeleteFile> logger,
            CancellationToken cancellationToken)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? currentUser.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                logger.LogWarning("Unauthorized delete attempt. TraceId: {TraceId}", traceId);
                return Results.Unauthorized();
            }

            var record = await dbContext.StoredFiles.FindAsync(new object[] { id }, cancellationToken);
            if (record == null)
            {
                logger.LogInformation("File {Id} not found for delete. TraceId: {TraceId}", id, traceId);
                return Results.NotFound(ApiResponse<string>.Fail("File not found.", traceId));
            }

                        // remove physical file
            await storage.DeleteFileAsync(record.Url, cancellationToken);

                        // remove record from database
            dbContext.StoredFiles.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} deleted file {FileId}. TraceId: {TraceId}", currentUserId, id, traceId);

            return Results.Ok(ApiResponse<string>.Ok(null, "File deleted.", traceId));
        }
    }
}