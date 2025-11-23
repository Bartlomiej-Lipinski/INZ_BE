using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Storage;

public class PostFile : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/entities/{entityType}/{entityId}/files", Handle)
            .WithName("PostFile")
            .WithDescription("Upload file and link it to an entity")
            .WithTags("Storage")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>()
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data");
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string entityType,
        [FromRoute] string entityId,
        [FromForm] IFormFile file,
        [FromForm] string? categoryId,
        AppDbContext dbContext,
        IStorageService storage,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostFile> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        if (!Enum.TryParse<EntityType>(entityType, true, out var parsedEntityType))
        {
            logger.LogWarning("Invalid entity type '{EntityType}' for user {UserId}. TraceId: {TraceId}",
                entityType, userId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Invalid entity type.", traceId));
        }

        if (file == null || file.Length == 0)
            return Results.BadRequest(ApiResponse<string>.Fail("No file uploaded.", traceId));

        FileCategory? category = null;
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            category = await dbContext.FileCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.GroupId == groupId, cancellationToken);

            if (category == null)
                return Results.BadRequest(ApiResponse<string>.Fail("Category does not exist in this group.", traceId));
        }
        
        string url;
        await using (var stream = file.OpenReadStream())
        {
            url = await storage.SaveFileAsync(stream, file.FileName, file.ContentType, cancellationToken);
        }

        var record = new StoredFile
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UploadedById = userId!,
            EntityType = parsedEntityType,
            EntityId = entityId,
            CategoryId = category?.Id,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            Url = url,
            UploadedAt = DateTime.UtcNow
        };

        await dbContext.StoredFiles.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} uploaded file {FileId} for {EntityType}/{EntityId}. TraceId: {TraceId}",
            userId, record.Id, entityType, entityId, traceId);

        var dto = new StoredFileResponseDto
        {
            Id = record.Id,
            FileName = record.FileName,
            ContentType = record.ContentType,
            Size = record.Size,
            Url = record.Url,
            EntityType = record.EntityType.ToString(),
            EntityId = record.EntityId,
            UploadedAt = record.UploadedAt,
            FileCategory = category != null 
                ? new FileCategoryResponseDto { Id = category.Id, Name = category.Name } 
                : null
        };

        return Results.Ok(ApiResponse<StoredFileResponseDto>.Ok(dto, "File uploaded.", traceId));
    }
}