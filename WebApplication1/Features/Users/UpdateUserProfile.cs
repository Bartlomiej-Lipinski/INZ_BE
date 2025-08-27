using Microsoft.AspNetCore.Mvc;
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.id))
        {
            return Results.BadRequest(ApiResponse<string>.Fail("User ID cannot be null or empty."));
        }

        var user = await dbContext.Users.FindAsync(request.id);
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
        public string id { get; init; } 
        public string? Name { get; init; }
        public string? Surname { get; init; }
        public DateOnly? BirthDate { get; init; }
        public string? Status { get; init; }
        public string? Description { get; init; }
        public string? Photo { get; init; }
    }
    
}