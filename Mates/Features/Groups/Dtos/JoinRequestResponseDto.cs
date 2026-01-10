using Mates.Features.Users.Dtos;

namespace Mates.Features.Groups.Dtos;

public record JoinRequestResponseDto
{
    public string GroupId { get; init; } = null!;
    public string GroupName { get; init; }
    public UserResponseDto User { get; set; } = null!;
}
