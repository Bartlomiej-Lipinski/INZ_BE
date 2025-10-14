namespace WebApplication1.Infrastructure.Data.Entities.Recommendations;

public class RecommendationReaction
{
    public string RecommendationId { get; set; } = null!;
    
    public string UserId { get; set; } = null!;
    
    public bool IsLiked { get; set; } = true;

    public Recommendation Recommendation { get; set; } = null!;
    public User User { get; set; } = null!;
}