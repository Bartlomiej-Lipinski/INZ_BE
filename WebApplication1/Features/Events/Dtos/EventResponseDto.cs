namespace WebApplication1.Features.Events.Dtos;

public record EventResponseDto
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsAutoScheduled { get; set; }
    public DateTime? RangeStart { get; set; }
    public DateTime? RangeEnd { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<EventAvailabilityResponseDto> Availabilities { get; set; } = [];
    public ICollection<EventSuggestionResponseDto> Suggestions { get; set; } = [];
}