namespace WebApplication1.Infrastructure.Data.Entities.Events;

public class EventAvailabilityRange
{
    public string Id { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; }
    
    public Event Event { get; set; } = null!;
    public User User { get; set; } = null!;
}