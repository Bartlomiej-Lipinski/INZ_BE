namespace WebApplication1.Infrastructure.Data.Entities.Recommendations;

public class RecommendationComment
{
    public string Id { get; set; } = null!;
    
    public string RecommendationId { get; set; } = null!;
    
    public string UserId { get; set; } = null!;
    
    public string Content { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Recommendation Recommendation { get; set; } = null!;
    
    public User User { get; set; } = null!;
}