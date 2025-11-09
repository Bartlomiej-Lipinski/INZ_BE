namespace WebApplication1.Infrastructure.Data.Entities.Challenges;

public class ChallengeParticipant
{
    public string ChallengeId { get; set; } = null!;
    public string UserId { get; set; } = null!;

    public int Points { get; set; }
    
    public Challenge Challenge { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<ChallengeProgress> ProgressEntries { get; set; } = new List<ChallengeProgress>();
}
