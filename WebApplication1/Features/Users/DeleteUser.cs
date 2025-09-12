using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Users;

[ApiExplorerSettings(GroupName = "Users")]
public class DeleteUser : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/users/{userName}", Handle)
            .WithName("DeleteUser")
            .WithDescription("Deletes a user by username")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string userName,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        HttpContext httpContext,
        ILogger<DeleteUser> logger)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        if (string.IsNullOrEmpty(userName))
        {
            logger.LogWarning("Invalid username provided for deletion. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("User name cannot be null or empty.", traceId));
        }

        logger.LogInformation("Attempting to delete user with username: {UserName}. TraceId: {TraceId}", userName, traceId);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User not found for deletion with username: {UserName}. TraceId: {TraceId}", userName, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("User not found.", traceId));
        }

        dbContext.Users.Remove(user);
        var deleted = await dbContext.SaveChangesAsync(cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation("User successfully deleted with username: {UserName}. TraceId: {TraceId}", userName, traceId);
            return Results.Ok(ApiResponse<string>.Ok("User deleted successfully.", null, traceId));
        }
        else
        {
            logger.LogError("Failed to delete user with username: {UserName}. TraceId: {TraceId}", userName, traceId);
            return Results.Json(ApiResponse<string>.Fail("Failed to delete user.", traceId), statusCode: 500);
        }
    }  
}