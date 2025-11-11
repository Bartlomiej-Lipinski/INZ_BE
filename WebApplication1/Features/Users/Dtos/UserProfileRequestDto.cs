using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Features.Users.Dtos;

public record UserProfileRequestDto
{
    [MaxLength(100)]
    public string Name { get; init; } = null!;
    [MaxLength(100)]
    public string Surname { get; init; } = null!;
    [DataType(DataType.Date)]
    public DateOnly? BirthDate { get; init; }
    [MaxLength(250)]
    public string? Status { get; init; }
    [MaxLength(300)]
    public string? Description { get; init; }
}