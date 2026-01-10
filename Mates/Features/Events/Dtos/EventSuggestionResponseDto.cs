namespace Mates.Features.Events.Dtos;

public record EventSuggestionResponseDto
{
    public string Id { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int AvailableUserCount { get; set; }
}