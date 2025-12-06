using WebApplication1.Features.Users.Dtos;

namespace WebApplication1.Features.Polls.Dtos;

public record PollOptionDto
{
    public string Id { get; set; }
    public string Text { get; set; } = null!;
    public List<UserResponseDto> VotedUsers { get; set; } = [];
}