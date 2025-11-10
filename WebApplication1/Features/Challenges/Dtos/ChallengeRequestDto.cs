namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeRequestDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double? PointsPerUnit { get; set; } = 1;
    public string? Unit { get; set; }
}