namespace WebApplication1.Features.Polls.Dtos;

public record PollVoteRequestDto
{
    public string OptionId { get; set; } = null!;
}