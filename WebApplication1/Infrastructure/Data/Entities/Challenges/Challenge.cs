using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Enums;

namespace WebApplication1.Infrastructure.Data.Entities.Challenges;

public class Challenge
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    
    public EntityType EntityType { get; set; }
    
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double PointsPerUnit { get; set; } = 1;
    public string Unit { get; set; }
    
    public bool IsCompleted { get; set; }
    
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<ChallengeParticipant> Participants { get; set; } = new List<ChallengeParticipant>();
    public ICollection<ChallengeStage> Stages { get; set; } = new List<ChallengeStage>();
}
