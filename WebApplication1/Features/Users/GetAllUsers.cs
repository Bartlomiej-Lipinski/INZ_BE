using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Users;
//TODO: Delete this code in production, this is just for testing purposes
[ApiExplorerSettings(GroupName = "Users")]
public class GetAllUsers : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", Handle)
            .WithName("GetAllUsers")
            .WithDescription("Returns all users")
            .WithTags("Users")
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetAllUsers> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        logger.LogInformation("Fetching all users. TraceId: {TraceId}", traceId);

        var users = await dbContext.Users
            .AsNoTracking()
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
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            logger.LogWarning("No users found. TraceId: {TraceId}", traceId);
            return Results.NotFound(ApiResponse<string>.Fail("No users found.", traceId));
        }

        logger.LogInformation("Successfully retrieved {UserCount} users. TraceId: {TraceId}", users.Count, traceId);
        return Results.Ok(ApiResponse<List<UserResponseDto>>.Ok(users, null, traceId));
    }

    private record UserResponseDto
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