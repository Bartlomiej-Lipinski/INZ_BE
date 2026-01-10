using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Infrastructure.Data.Enums;

namespace Mates.Infrastructure.Data.Entities.Challenges;

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
    public string GoalUnit { get; set; } = null!;
    public double GoalValue { get; set; }
    
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<ChallengeParticipant> Participants { get; set; } = new List<ChallengeParticipant>();
}
