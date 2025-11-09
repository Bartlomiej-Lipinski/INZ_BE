using WebApplication1.Infrastructure.Data.Enums;

namespace WebApplication1.Infrastructure.Data.Entities.Comments;

public class Reaction
{
    public string TargetId { get; set; } = null!;
    
    public EntityType EntityType { get; set; }
    
    public string UserId { get; set; } = null!;
    
    public User User { get; set; } = null!;
}