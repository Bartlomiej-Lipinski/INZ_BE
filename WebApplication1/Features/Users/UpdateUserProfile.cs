using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;


namespace WebApplication1.Features.Users;

public class UpdateUserProfile:IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/profile", Handle)
            .WithName("UpdateUserProfile")
            .WithDescription("Updates a user's profile")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        ClaimsPrincipal currentUser,
        [FromBody] UpdateUserProfileRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        HttpContext httpContext,
        ILogger<UpdateUserProfile> logger)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Invalid user ID provided for profile update. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("User ID cannot be null or empty.", traceId));
        }

        logger.LogInformation("Attempting to update profile for user ID: {UserId}. TraceId: {TraceId}", userId, traceId);

        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            logger.LogWarning("User not found for profile update with ID: {UserId}. TraceId: {TraceId}", userId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("User not found.", traceId));
        }

        user.Name = request.Name;
        user.Surname = request.Surname;
        user.BirthDate = request.BirthDate;
        user.Status = request.Status;
        user.Description = request.Description;
        user.Photo = request.Photo;

        dbContext.Users.Update(user);
        var updated = await dbContext.SaveChangesAsync(cancellationToken);

        if (updated > 0)
        {
            logger.LogInformation("User profile successfully updated for ID: {UserId}. TraceId: {TraceId}", userId, traceId);
            return Results.Ok(ApiResponse<string>.Ok("User profile updated successfully.", traceId));
        }
        else
        {
            logger.LogError("Failed to update user profile for ID: {UserId}. TraceId: {TraceId}", userId, traceId);
            return Results.Json(ApiResponse<string>.Fail("Failed to update user profile.", traceId), statusCode: 500);
        }
    }
    
    public record UpdateUserProfileRequest
    {
        [MaxLength(100)]
        public string? Name { get; init; }
        [MaxLength(100)]
        public string? Surname { get; init; }
        [DataType(DataType.Date)]
        public DateOnly? BirthDate { get; init; }
        [MaxLength(250)]
        public string? Status { get; init; }
        [MaxLength(300)]
        public string? Description { get; init; }
        [MaxLength(500)]
        public string? Photo { get; init; }
    }
    
}