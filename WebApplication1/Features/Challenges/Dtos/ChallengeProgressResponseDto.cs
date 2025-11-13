namespace WebApplication1.Features.Challenges.Dtos;

public record ChallengeProgressResponseDto
{
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public double Value { get; set; }
}