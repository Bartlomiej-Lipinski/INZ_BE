using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups.GroupCRUD;

[ApiExplorerSettings(GroupName = "Groups")]
public class PostGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups", Handle)
            .WithName("CreateGroup")
            .WithDescription("Creates a new group and assigns the current user as admin")
            .WithTags("Groups")
            .RequireAuthorization();
    }
    
    public static async Task<IResult> Handle(
        HttpContext httpContext, 
        [FromBody] GroupRequestDto requestDto,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        ILogger<PostGroup> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to create group. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(requestDto.Name) || string.IsNullOrWhiteSpace(requestDto.Color))
        {
            logger.LogWarning("Invalid group data provided. Name: {Name}, Color: {Color}. TraceId: {TraceId}", 
                requestDto.Name, requestDto.Color, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Name and Color are required", traceId));
        }

        logger.LogInformation("Creating new group '{GroupName}' for user {UserId}. TraceId: {TraceId}", 
            requestDto.Name, userId, traceId);
        
        var group = new Group
        {
            Id = Guid.NewGuid().ToString(),
            Name = requestDto.Name,
            Color = requestDto.Color,
            Code = Guid.NewGuid().ToString()[..8].ToUpper()
        };

        await dbContext.Groups.AddAsync(group, cancellationToken);
        
        var groupUser = new GroupUser
        {
            GroupId = group.Id,
            UserId = userId,
            IsAdmin = true
        };

        await dbContext.GroupUsers.AddAsync(groupUser, cancellationToken);
        var saved = await dbContext.SaveChangesAsync(cancellationToken);
        
        if (saved > 0)
        {
            var response = new GroupResponseDto
            {
                Id = group.Id,
                Name = group.Name,
                Color = group.Color,
            };

            logger.LogInformation("Group '{GroupName}' successfully created with ID: {GroupId}. TraceId: {TraceId}", 
                group.Name, group.Id, traceId);
            return Results.Created($"/groups/{group.Id}", ApiResponse<GroupResponseDto>
                .Ok(response, null, traceId));
        }

        logger.LogError("Failed to create group '{GroupName}' for user {UserId}. TraceId: {TraceId}", 
            requestDto.Name, userId, traceId);
        return Results.Json(ApiResponse<string>.Fail("Failed to create group", traceId), statusCode: 500);
    }
}