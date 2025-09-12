using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Users;

public class UpdateUserProfile:IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/{id}/profile", Handle)
            .WithName("UpdateUserProfile")
            .WithDescription("Updates a user's profile")
            .WithTags("Users")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromBody] UpdateUserProfileRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        HttpContext httpContext,
        ILogger<UpdateUserProfile> logger)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        if (string.IsNullOrWhiteSpace(request.id))
        {
            logger.LogWarning("Invalid user ID provided for profile update. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("User ID cannot be null or empty.", traceId));
        }

        logger.LogInformation("Attempting to update profile for user ID: {UserId}. TraceId: {TraceId}", request.id, traceId);

        var user = await dbContext.Users.FindAsync(request.id);
        if (user == null)
        {
            logger.LogWarning("User not found for profile update with ID: {UserId}. TraceId: {TraceId}", request.id, traceId);
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
            logger.LogInformation("User profile successfully updated for ID: {UserId}. TraceId: {TraceId}", request.id, traceId);
            return Results.Ok(ApiResponse<string>.Ok("User profile updated successfully.", traceId));
        }
        else
        {
            logger.LogError("Failed to update user profile for ID: {UserId}. TraceId: {TraceId}", request.id, traceId);
            return Results.Json(ApiResponse<string>.Fail("Failed to update user profile.", traceId), statusCode: 500);
        }
    }
    
    public record UpdateUserProfileRequest
    {
        [Required]
        [MaxLength(50)]
        public string id { get; init; } 
        public string? Name { get; init; }
        public string? Surname { get; init; }
        public DateOnly? BirthDate { get; init; }
        public string? Status { get; init; }
        public string? Description { get; init; }
        public string? Photo { get; init; }
    }
    
}