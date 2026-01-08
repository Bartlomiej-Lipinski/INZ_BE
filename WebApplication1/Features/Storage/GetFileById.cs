using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;

namespace WebApplication1.Features.Storage;

public class GetFileById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files/{id}", Handle)
            .WithName("GetFileById")
            .WithDescription("Download file by id")
            .WithTags("Storage")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string id,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetFileById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        var record = await dbContext.StoredFiles.FindAsync([id], cancellationToken);
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

        var hasAccess = record.UploadedById == userId; // Własny plik

        if (!hasAccess)
        {
            var isProfilePicture = await dbContext.StoredFiles
                .AsNoTracking()
                .Where(f => f.Id == id)
                .AnyAsync(f => f.UploadedBy != null, cancellationToken);

            if (isProfilePicture) hasAccess = true;
        }

        if (!hasAccess && !string.IsNullOrEmpty(record.GroupId))
            hasAccess = await dbContext.GroupUsers
                .AsNoTracking()
                .AnyAsync(gu => gu.GroupId == record.GroupId && gu.UserId == userId, cancellationToken);

        if (!hasAccess)
        {
            logger.LogError("User attempted to access file {Id} without permission. TraceId: {TraceId}", id, traceId);
            return Results.Forbid();
        }

        logger.LogInformation("Serving file {Id} to request. TraceId: {TraceId}", id, traceId);

        var contentType = string.IsNullOrWhiteSpace(record.ContentType)
            ? "application/octet-stream"
            : record.ContentType + "; charset=utf-16";
        return Results.File(stream, contentType, record.FileName, enableRangeProcessing: true);
    }
}