namespace WebApplication1.Infrastructure.Data.Entities.Challenges;

public class ChallengePost
{
    public string Id { get; set; } = null!;
    public string ChallengeId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Challenge Challenge { get; set; } = null!;
    public User User { get; set; } = null!;
}