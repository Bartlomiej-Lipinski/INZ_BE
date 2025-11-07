using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Features.Timeline.Dtos;

public record TimelineEventResponseDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public EventType Type { get; set; }
}