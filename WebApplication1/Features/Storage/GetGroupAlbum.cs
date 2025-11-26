using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Storage;

public class GetGroupAlbum : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/album", Handle)
            .WithName("GetGroupAlbum")
            .WithDescription("Retrieves all files in the group's album")
            .WithTags("Storage")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupAlbum> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} started fetching album for group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);
        
        var album = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => f.GroupId == groupId && f.EntityType == EntityType.AlbumMedia)
            .Select(f => new StoredFileResponseDto
            {
                Id = f.Id,
                FileName = f.FileName,
                ContentType = f.ContentType,
                Size = f.Size,
                Url = f.Url,
                EntityType = f.EntityType.ToString(),
                UploadedAt = f.UploadedAt
            })
            .ToListAsync(cancellationToken);
        
        if (album.Count == 0)
        {
            logger.LogInformation("No album found for group {GroupId}. TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<List<StoredFileResponseDto>>
                .Ok(album, "No album found for this group.", traceId));
        }
        
        logger.LogInformation("User {UserId} retrieved {Count} album for group {GroupId}. TraceId: {TraceId}",
            userId, album.Count, groupId, traceId);
        return Results.Ok(ApiResponse<List<StoredFileResponseDto>>.Ok(album, "Group album retrieved successfully.",
            traceId));
    }
}