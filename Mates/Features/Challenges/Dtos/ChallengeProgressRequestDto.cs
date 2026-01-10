namespace Mates.Features.Challenges.Dtos;

public record ChallengeProgressRequestDto
{
    public string? Description { get; set; }
    public double Value { get; set; }
}