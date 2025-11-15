namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeRequestDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GoalUnit { get; set; } = null!;
    public double GoalValue { get; set; }
    public bool? IsCompleted { get; set; }
}