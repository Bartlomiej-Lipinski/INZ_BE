using WebApplication1.Features.Users.Dtos;

namespace WebApplication1.Features.Polls.Dtos;

public record PollOptionDto
{
    public string Text { get; set; } = null!;
    public ICollection<UserResponseDto> VotedUsers { get; set; } = [];
}