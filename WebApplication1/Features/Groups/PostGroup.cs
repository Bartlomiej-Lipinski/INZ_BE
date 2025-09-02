using System.ComponentModel.DataAnnotations;
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Name) || string.IsNullOrWhiteSpace(requestDto.Color))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Name and Color are required"));
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        
        var group = new Group
        {
            Id = Guid.NewGuid().ToString(),
            Name = requestDto.Name,
            Color = requestDto.Color
        };

        await dbContext.Groups.AddAsync(group, cancellationToken);
        
        var groupUser = new GroupUser
        {
            GroupId = group.Id,
            UserId = userId,
            IsAdmin = true
        };

        await dbContext.GroupUsers.AddAsync(groupUser, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new GroupResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Color = group.Color,
            Code = group.Code
        };

        return Results.Created($"/groups/{group.Id}", ApiResponse<GroupResponseDto>.Ok(response));
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
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Color { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}