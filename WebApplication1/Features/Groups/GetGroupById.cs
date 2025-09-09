using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

[ApiExplorerSettings(GroupName = "Groups")]
public class GetGroupById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{id}", Handle)
            .WithName("GetGroupById")
            .WithDescription("Returns a group by ID")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string id,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group == null)
            return Results.NotFound(ApiResponse<string>.Fail("Group not found"));

        var dto = new GroupResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Color = group.Color,
            Code = group.Code
        };

        return Results.Ok(ApiResponse<GroupResponseDto>.Ok(dto));
    }

    public class GroupResponseDto
    {
        [MaxLength(50)]
        public string Id { get; set; } = null!;
        [MaxLength(50)]
        public string Name { get; set; } = null!;
        [MaxLength(7)]
        public string Color { get; set; } = null!;
        [MaxLength(5)]
        public string Code { get; set; } = null!;
    }
}