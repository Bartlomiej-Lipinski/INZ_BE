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
    
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<RecommendationComment> Comments { get; set; } = new List<RecommendationComment>();
    public ICollection<RecommendationReaction> Reactions { get; set; } = new List<RecommendationReaction>();
}