using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Users;

[ApiExplorerSettings(GroupName = "Users")]
public class DeleteUser : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/users", Handle)
            .WithName("DeleteUser")
            .WithDescription("Deletes a user by username")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return Results.BadRequest(ApiResponse<string>.Fail("User name cannot be null or empty."));

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user == null)
            return Results.NotFound(ApiResponse<string>.Fail("User not found."));

        dbContext.Users.Remove(user);
        var deleted = await dbContext.SaveChangesAsync(cancellationToken);

        return deleted > 0
            ? Results.Ok(ApiResponse<string>.Ok("User deleted successfully."))
            : Results.Json(ApiResponse<string>.Fail("Failed to delete user."), statusCode: 500);
    }  
}