namespace WebApplication1.Features.Events.Dtos;

public record EventSuggestionResponseDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int AvailableUserCount { get; set; }
}