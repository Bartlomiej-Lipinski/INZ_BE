using WebApplication1.Features.Users.Dtos;

namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeParticipantResponseDto
{
    public UserResponseDto User { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<ChallengeProgressResponseDto> ProgressEntries { get; set; } = [];
}