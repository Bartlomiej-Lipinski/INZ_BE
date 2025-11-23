using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Storage;

public class PostFileCategory: IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/categories", Handle)
            .WithName("PostFileCategory")
            .WithDescription("Create a new file category in a group")
            .WithTags("Storage")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] string name,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostFileCategory> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} started creating a category in group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);

        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Category creation failed: name is required. User {UserId}, Group {GroupId}, TraceId: {TraceId}",
                userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Category name is required.", traceId));
        }
        
        var category = new FileCategory
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            Name = name
        };
        
        dbContext.FileCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} created category {CategoryId} for group {GroupId}. TraceId: {TraceId}",
            userId, category.Id, groupId, traceId);

        return Results.Ok(ApiResponse<FileCategory>.Ok(category, "Category created.", traceId));
    }
}