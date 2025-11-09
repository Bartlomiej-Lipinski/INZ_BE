namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeStageResponseDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Order { get; set; }
}