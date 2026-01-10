using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Storage.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Storage;

public class GetGroupMaterials : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/materials", Handle)
            .WithName("GetGroupMaterials")
            .WithDescription("Retrieves all materials for a specific group with optional filters")
            .WithTags("Storage")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromQuery] string? categoryId,
        [FromQuery] string? uploadedById,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GetGroupMaterials> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} started fetching materials for group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);
        
        var query = dbContext.StoredFiles
            .AsNoTracking()
            .Include(f => f.FileCategory)
            .Where(f => f.GroupId == groupId && f.EntityType == EntityType.Material);
        
        if (!string.IsNullOrWhiteSpace(categoryId))
            query = query.Where(f => f.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(uploadedById))
            query = query.Where(f => f.UploadedById == uploadedById);

        var materials = await query
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new StoredFileResponseDto
            {
                Id = f.Id,
                FileName = f.FileName,
                ContentType = f.ContentType,
                Size = f.Size,
                Url = f.Url,
                EntityType = f.EntityType.ToString(),
                UploadedAt = f.UploadedAt,
                FileCategory = f.FileCategory != null ? new FileCategoryResponseDto
                {
                    Id = f.FileCategory.Id, 
                    Name = f.FileCategory.Name
                } : null
            })
            .ToListAsync(cancellationToken);
        
        if (materials.Count == 0)
        {
            logger.LogInformation("No materials found for group {GroupId}. TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<List<StoredFileResponseDto>>
                .Ok(materials, "No materials found for this group.", traceId));
        }
        
        logger.LogInformation("User {UserId} retrieved {Count} materials for group {GroupId}. TraceId: {TraceId}",
            userId, materials.Count, groupId, traceId);
        return Results.Ok(ApiResponse<List<StoredFileResponseDto>>.Ok(materials, "Group materials retrieved successfully.",
            traceId));
    }
}