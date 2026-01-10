using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Infrastructure.Service;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Mates.Shared.Extensions;

namespace Mates.Features.Storage;

public class DeleteFile : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/files/{id}", Handle)
            .WithName("DeleteFile")
            .WithDescription("Delete file by id")
            .WithTags("Storage")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string id,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteFile> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        var record = await dbContext.StoredFiles.FindAsync([id], cancellationToken);
        if (record == null)
        {
            logger.LogInformation("File {Id} not found for delete. TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("File not found.", traceId));
        }
        
        var groupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = groupUser?.IsAdmin ?? false;
        if (record.UploadedById != userId && !isAdmin)
        {
            logger.LogWarning("User {UserId} attempted to delete file {FileId} they do not own and is not admin. " +
                              "TraceId: {TraceId}", userId, id, traceId);
            return Results.Forbid();
        }

        await storage.DeleteFileAsync(record.Url, cancellationToken);

        dbContext.StoredFiles.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted file {FileId}. TraceId: {TraceId}", userId, id, traceId);

        return Results.Ok(ApiResponse<string>.Ok(null!, "File deleted.", traceId));
    }
}