namespace WebApplication1.Infrastructure.Data.Entities.Events;

public class EventSuggestion
{
    public string Id { get; set; } = null!;
    public string EventId { get; set; } = null!;
    
    public DateTime StartTime { get; set; }
    
    public int AvailableUserCount { get; set; }

    public Event Event { get; set; } = null!;
}