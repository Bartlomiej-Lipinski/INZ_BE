using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;

namespace WebApplication1.Features.Storage
{
    public class GetFile : IEndpoint
    {
        public void RegisterEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/files/{id}", Handle)
                .WithName("GetFile")
                .WithDescription("Download file by id")
                .WithTags("Storage")
                .RequireAuthorization()
                .WithOpenApi();
        }

        public static async Task<IResult> Handle(
            [FromRoute] string id,
            AppDbContext dbContext,
            IStorageService storage,
            HttpContext httpContext,
            ILogger<GetFile> logger,
            CancellationToken cancellationToken)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

            var record = await dbContext.StoredFiles.FindAsync(new object[] { id }, cancellationToken);
            if (record == null)
            {
                logger.LogInformation("File {Id} not found. TraceId: {TraceId}", id, traceId);
                return Results.NotFound();
            }

            var stream = await storage.OpenReadAsync(record.Url, cancellationToken);
            if (stream == null)
            {
                logger.LogInformation("Physical file for {Id} not found. TraceId: {TraceId}", id, traceId);
                return Results.NotFound();
            }

            logger.LogInformation("Serving file {Id} to request. TraceId: {TraceId}", id, traceId);

            var contentType = string.IsNullOrWhiteSpace(record.ContentType) ? "application/octet-stream" : record.ContentType;
            return Results.File(stream, contentType, record.FileName, enableRangeProcessing: true);
        }
    }
}