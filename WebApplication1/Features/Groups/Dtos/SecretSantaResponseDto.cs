using WebApplication1.Features.Users.Dtos;

namespace WebApplication1.Features.Groups.Dtos;

public record SecretSantaResponseDto
{
    public UserResponseDto Giver { get; set; } = null!;
    public UserResponseDto Receiver { get; set; } = null!;
}