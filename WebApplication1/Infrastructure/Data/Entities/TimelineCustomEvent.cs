using WebApplication1.Infrastructure.Data.Entities.Groups;

namespace WebApplication1.Infrastructure.Data.Entities;

public record TimelineCustomEvent
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string CreatedByUserId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}