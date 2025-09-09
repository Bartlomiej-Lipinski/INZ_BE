using Microsoft.AspNetCore.Mvc;
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
        CancellationToken cancellationToken)
    {
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("User ID cannot be null or empty."));
        }

        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return Results.NotFound(ApiResponse<string>.Fail("User not found."));
        }

        user.Name = request.Name;
        user.Surname = request.Surname;
        user.BirthDate = request.BirthDate;
        user.Status = request.Status;
        user.Description = request.Description;
        user.Photo = request.Photo;

        dbContext.Users.Update(user);
        var updated = await dbContext.SaveChangesAsync(cancellationToken);

        return updated > 0
            ? Results.Ok(ApiResponse<string>.Ok("User profile updated successfully."))
            : Results.Json(ApiResponse<string>.Fail("Failed to update user profile."), statusCode: 500);
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