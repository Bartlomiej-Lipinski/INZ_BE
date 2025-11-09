namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeParticipantResponseDto
{
    public string UserId { get; set; } = null!;
    public int? Points { get; set; }
}