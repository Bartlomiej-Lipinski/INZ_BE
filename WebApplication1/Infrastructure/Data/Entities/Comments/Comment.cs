using WebApplication1.Infrastructure.Data.Enums;

namespace WebApplication1.Infrastructure.Data.Entities.Comments;

public class Comment
{
    public string Id { get; set; } = null!;
    public string TargetId { get; set; } = null!;
    
    public EntityType EntityType { get; set; }
    public string UserId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    
    public User User { get; set; } = null!;
}