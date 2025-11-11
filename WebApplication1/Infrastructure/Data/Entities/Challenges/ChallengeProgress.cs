namespace WebApplication1.Infrastructure.Data.Entities.Challenges;

public class ChallengeProgress
{
    public string Id { get; set; } = null!;
    public string ChallengeId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    
    public DateTime Date { get; set; }
    public string Description { get; set; } = null!;
    public double Value { get; set; }
    
    public ChallengeParticipant Participant { get; set; } = null!;
}