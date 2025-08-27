using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
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
    public static async Task<IResult> Handle(AppDbContext context,CancellationToken dbc)
    {
        var groups = await context.Groups.ToListAsync(dbc);
        return Results.Ok(groups);
    }
}