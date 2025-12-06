using WebApplication1.Features.Users.Dtos;

namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeResponseDto
{
    public string Id { get; set; } = null!;
    public UserResponseDto User { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GoalUnit { get; set; } = null!;
    public double GoalValue { get; set; }
    public bool IsCompleted { get; set; }
    public List<ChallengeParticipantResponseDto> Participants { get; set; } = [];
}