using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Storage.Categories;

public class DeleteFileCategory: IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/categories/{categoryId}", Handle)
            .WithName("DeleteFileCategory")
            .WithDescription("Delete a file category in a group")
            .WithTags("Storage")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string categoryId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteFileCategory> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} started deleting category {CategoryId} in group {GroupId}. TraceId: {TraceId}",
            userId, categoryId, groupId, traceId);

        var category = await dbContext.FileCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.GroupId == groupId, cancellationToken);

        if (category == null)
        {
            logger.LogWarning("Category {CategoryId} not found in group {GroupId}. TraceId: {TraceId}",
                categoryId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Category not found.", traceId));
        }
        
        var groupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = groupUser?.IsAdmin ?? false;
        if (!isAdmin)
        {
            logger.LogWarning("User {UserId} attempted to delete category {CategoryId} without admin privileges. TraceId: {TraceId}",
                userId, categoryId, traceId);
            return Results.Forbid();
        }

        var files = await dbContext.StoredFiles
            .Where(f => f.CategoryId == categoryId)
            .ToListAsync(cancellationToken);

        foreach (var file in files)
        {
            file.CategoryId = null;
        }
        
        dbContext.FileCategories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Category {CategoryId} deleted by user {UserId}. TraceId: {TraceId}",
            categoryId, userId, traceId);

        return Results.Ok(ApiResponse<string>.Ok(null!, "Category deleted.", traceId));
    }
}