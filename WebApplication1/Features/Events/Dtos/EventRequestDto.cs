using WebApplication1.Infrastructure.Data.Entities.Events;

namespace WebApplication1.Features.Events.Dtos;

public record EventRequestDto
{
    public string Title { get; set; } 
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsAutoScheduled { get; set; }
    public DateTime? RangeStart { get; set; }
    public DateTime? RangeEnd { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public EventStatus Status { get; set; }
}