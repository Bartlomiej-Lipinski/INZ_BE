using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Users;

[ApiExplorerSettings(GroupName = "Users")]
public class GetUserById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/{id}", Handle)
            .WithName("GetUserById")
            .WithDescription("Returns a user by ID")
            .WithTags("Users")
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
            return Results.BadRequest(ApiResponse<string>.Fail("User ID cannot be null or empty."));
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                UserName = u.UserName!,
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
            return Results.NotFound(ApiResponse<string>.Fail("User not found."));
        }

        return Results.Ok(ApiResponse<UserResponseDto>.Ok(user));
    }

    public class UserResponseDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? Photo { get; set; }
    }
}