namespace Mates.Features.Polls.Dtos;

public record PollRequestDto
{
    public string Question { get; set; } = null!;
    public List<PollOptionDto> Options { get; set; } = [];
}