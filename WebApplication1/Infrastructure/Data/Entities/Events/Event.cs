using WebApplication1.Infrastructure.Data.Entities.Groups;

namespace WebApplication1.Infrastructure.Data.Entities.Events;

public class Event
{
    public string Id { get; set; } = null!;
    
    public string GroupId { get; set; } = null!;
    
    public string UserId { get; set; } = null!;
    
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public string? Location { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public Group Group { get; set; } = null!;
    
    public User User { get; set; } = null!;

    public ICollection<EventAvailability> Availabilities { get; set; } = new List<EventAvailability>();
}