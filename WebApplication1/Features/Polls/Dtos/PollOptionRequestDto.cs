namespace WebApplication1.Features.Polls.Dtos;

public record PollOptionRequestDto
{
    public string Text { get; set; } = null!;
}