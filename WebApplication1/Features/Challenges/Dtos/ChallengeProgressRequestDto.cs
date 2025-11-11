namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeProgressRequestDto
{
    public string Description { get; set; } = null!;
    public double Value { get; set; }
}