using Mates.Features.Users.Dtos;

namespace Mates.Features.Groups.Dtos;

public record SecretSantaResponseDto
{
    public UserResponseDto Giver { get; set; } = null!;
    public UserResponseDto Receiver { get; set; } = null!;
}