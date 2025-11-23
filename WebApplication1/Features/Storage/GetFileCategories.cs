using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Storage;

public class GetFileCategories : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/categories", Handle)
            .WithName("GetFileCategories")
            .WithDescription("Get all file categories for a group")
            .WithTags("Storage")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        HttpContext httpContext,
        ClaimsPrincipal currentUser,
        ILogger<GetFileCategories> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} fetching categories for group {GroupId}. TraceId: {TraceId}", 
            userId, groupId, traceId);

        var categories = await dbContext.FileCategories
            .Where(c => c.GroupId == groupId)
            .Select(c => new FileCategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync(cancellationToken);
        
        if (categories.Count == 0)
        {
            logger.LogInformation("No categories found for group {GroupId}. TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<List<FileCategoryResponseDto>>
                .Ok(categories, "No categories found for this group.", traceId));
        }
        
        logger.LogInformation("User {UserId} retrieved {Count} categories for group {GroupId}. TraceId: {TraceId}",
            userId, categories.Count, groupId, traceId);
        return Results.Ok(ApiResponse<List<FileCategoryResponseDto>>.Ok(categories, "Categories retrieved.", traceId));
    }
}