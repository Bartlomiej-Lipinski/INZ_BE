using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Infrastructure.Data.Entities.Groups;

namespace WebApplication1.Infrastructure.Data.Entities.Recommendations;

public class Recommendation
{
    public string Id { get; set; } = null!;
    
    public string GroupId { get; set; } = null!;
    
    public string UserId { get; set; } = null!;
    
    public string Title { get; set; } = null!;
    
    public string Content { get; set; } = null!;
    
    public string? Category { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public string? LinkUrl { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public Group Group { get; set; } = null!;
    
    public User User { get; set; } = null!;
}