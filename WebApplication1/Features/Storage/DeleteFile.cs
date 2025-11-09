using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Storage;

public class DeleteFile : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/files/{id}", Handle)
            .WithName("DeleteFile")
            .WithDescription("Delete file by id")
            .WithTags("Storage")
            .RequireAuthorization();
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
        var currentUserId = currentUser.GetUserId();

        var record = await dbContext.StoredFiles.FindAsync([id], cancellationToken);
        if (record == null)
        {
            logger.LogInformation("File {Id} not found for delete. TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("File not found.", traceId));
        }

        await storage.DeleteFileAsync(record.Url, cancellationToken);

        dbContext.StoredFiles.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted file {FileId}. TraceId: {TraceId}", currentUserId, id, traceId);

        return Results.Ok(ApiResponse<string>.Ok(null!, "File deleted.", traceId));
    }
}