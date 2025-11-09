namespace WebApplication1.Infrastructure.Data.Entities.Challenges;

public class ChallengeStage
{
    public string Id { get; set; } = null!;
    public string ChallengeId { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Order { get; set; }
    
    public Challenge Challenge { get; set; } = null!;
}