namespace WebApplication1.Features.Users.Dtos;

public record UserProfileRequestDto
{
    public string Name { get; init; } = null!;
    public string Surname { get; init; } = null!;
    public DateOnly? BirthDate { get; init; }
    public string? Status { get; init; }
    public string? Description { get; init; }
    public string? UserName { get; set; }
}