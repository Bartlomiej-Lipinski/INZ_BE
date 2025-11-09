namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeParticipantResponseDto
{
    public string UserId { get; set; } = null!;
    public int? Points { get; set; }
    public DateTime JoinedAt { get; set; }
    public List<ChallengeProgressResponseDto> ProgressEntries { get; set; } = [];
}