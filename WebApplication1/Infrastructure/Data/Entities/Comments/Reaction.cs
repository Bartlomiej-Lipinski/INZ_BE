namespace WebApplication1.Infrastructure.Data.Entities.Comments;

public class Reaction
{
    public string TargetId { get; set; } = null!;
    
    public string TargetType { get; set; } = null!;
    
    public string UserId { get; set; } = null!;
    
    public User User { get; set; } = null!;
}