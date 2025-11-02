namespace WebApplication1.Features.Polls.Dtos;

public record PollRequestDto
{
    public string Question { get; set; } = null!;
    public List<PollOptionRequestDto> Options { get; set; } = [];
}