namespace WebApplication1.Infrastructure.Data.Entities.Events;

public class EventAvailability
{
    public string EventId { get; set; } = null!;
    
    public string UserId { get; set; } = null!;
    
    public EventAvailabilityStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public Event Event { get; set; } = null!;

    public User User { get; set; } = null!;
}

public enum EventAvailabilityStatus
{
    Going = 1,
    NotGoing = 2,
    Maybe = 3
}