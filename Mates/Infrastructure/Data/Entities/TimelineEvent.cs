using Mates.Infrastructure.Data.Entities.Groups;

namespace Mates.Infrastructure.Data.Entities;

public record TimelineEvent
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    
    public string Title { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public EventType Type { get; set; }
    
    public Group Group { get; set; } = null!;
}

public enum EventType
{
    Birthday,
    GroupEvent,
    ImportantDate
}