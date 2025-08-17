using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GenerateCodeToJoinGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{id}/GenerateCodeToJoinGroup", Handle)
            .WithName("GenerateCodeToJoinGroup")
            .WithDescription("Generates a new code to join a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string id,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("Group ID cannot be null or empty."));
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("Group not found."));
        }

        group.Code = GenerateUniqueCode(dbContext, group.Id);
        group.codeExpiration = DateTime.UtcNow.AddMinutes(5);
        dbContext.Groups.Update(group);
        var updated = await dbContext.SaveChangesAsync(cancellationToken);

        if (updated > 0)
        {
            return Results.Ok(ApiResponse<string>.Ok("Code generated successfully."));
        }

        return Results.Json(ApiResponse<string>.Fail("Failed to generate code."), statusCode: 500);
    }
    private static string GenerateUniqueCode(AppDbContext dbContext, string groupId)
    {
        string code;
        do
        {
            var random = new Random();
            code = random.Next(10000, 99999).ToString();
        } while (dbContext.Groups.Any(g => g.Code == code && g.Id != groupId) 
                 || dbContext.Groups.Any(g => g.codeExpiration > DateTime.UtcNow && g.Code == code)
                 );

        return code;
    }
}