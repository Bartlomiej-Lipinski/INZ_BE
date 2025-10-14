using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

//TODO: Delete this endpoint in production
namespace WebApplication1.Features.Groups;
[AllowAnonymous]
public class GetGroups : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups", Handle)
            .WithName("GetGroups")
            .WithDescription("Returns all groups")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(AppDbContext context, CancellationToken dbc)
    {
        var groups = await context.Groups.AsNoTracking()
            .Select(g => new PostGroup.GroupResponseDto
            {
                Id = g.Id,
                Name = g.Name,
                Color = g.Color
            }).ToListAsync(dbc);;
        return Results.Ok(ApiResponse<List<PostGroup.GroupResponseDto>>.Ok(groups, "Groups retrieved successfully"));
    }
}