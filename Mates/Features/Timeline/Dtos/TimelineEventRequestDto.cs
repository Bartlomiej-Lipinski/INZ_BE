namespace Mates.Features.Timeline.Dtos;

public record TimelineEventRequestDto
{
    public string Title { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}