namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeResponseDto
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double? PointsPerUnit { get; set; } = 1;
    public string? Unit { get; set; }
    public bool IsCompleted { get; set; }
    public List<ChallengeParticipantResponseDto> Participants { get; set; } = [];
    public List<ChallengeStageResponseDto> Stages { get; set; } = [];
}