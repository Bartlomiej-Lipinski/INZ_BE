using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

[ApiExplorerSettings(GroupName = "Groups")]
public class PostGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups", Handle)
            .WithName("CreateGroup")
            .WithDescription("Creates a new group and assigns the current user as admin")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        HttpContext httpContext, 
        [FromBody] GroupRequestDto requestDto,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        ILogger<PostGroup> logger)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(requestDto.Name) || string.IsNullOrWhiteSpace(requestDto.Color))
        {
            logger.LogWarning("Invalid group data provided. Name: {Name}, Color: {Color}. TraceId: {TraceId}", 
                requestDto.Name, requestDto.Color, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Name and Color are required", traceId));
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("Unauthorized attempt to create group. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
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
            return Results.Created($"/groups/{group.Id}", ApiResponse<GroupResponseDto>.Ok(response, null, traceId));
        }

        logger.LogError("Failed to create group '{GroupName}' for user {UserId}. TraceId: {TraceId}", 
            requestDto.Name, userId, traceId);
        return Results.Json(ApiResponse<string>.Fail("Failed to create group", traceId), statusCode: 500);
    }

    public class GroupRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Color { get; set; } = null!;
    }

    public class GroupResponseDto
    {
        [MaxLength(50)]
        public string Id { get; set; } = null!;
        [MaxLength(50)]
        public string Name { get; set; } = null!;
        [MaxLength(7)]
        public string Color { get; set; } = null!;
        
    }
}