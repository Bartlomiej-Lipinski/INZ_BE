using Mates.Features.Storage.Dtos;

namespace Mates.Features.Users.Dtos;

public record UserResponseDto
{
    public string Id { get; set; } = null!;
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public ProfilePictureResponseDto? ProfilePicture { get; set; }
    public bool? IsTwoFactorEnabled { get; set; }
}