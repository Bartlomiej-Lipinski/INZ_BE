using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Users;

public class GetUserById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/{id}", Handle)
            .WithName("GetUserById")
            .WithDescription("Returns a user by ID")
            .WithTags("Users")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string id,
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetUserById> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var currentUserId = currentUser.GetUserId();
        
        if (currentUserId != id)
        {
            logger.LogWarning("Unauthorized access attempt to user ID: {UserId}. TraceId: {TraceId}", id, traceId);
            return Results.Forbid();
        }

        logger.LogInformation("Fetching user with ID: {UserId}. TraceId: {TraceId}", id, traceId);

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == currentUserId)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Username = u.UserName!,
                Email = u.Email!,
                Name = u.Name,
                Surname = u.Surname,
                BirthDate = u.BirthDate,
                Status = u.Status,
                Description = u.Description,
                Photo = u.Photo
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            logger.LogWarning("User not found with ID: {UserId}. TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("User not found.", traceId));
        }

        logger.LogInformation("User successfully retrieved with ID: {UserId}. TraceId: {TraceId}", id, traceId);
        return Results.Ok(ApiResponse<UserResponseDto>.Ok(user, null, traceId));
    }
}