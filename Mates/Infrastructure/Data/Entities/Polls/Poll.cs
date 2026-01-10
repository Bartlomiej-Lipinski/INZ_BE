using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Infrastructure.Data.Enums;

namespace Mates.Infrastructure.Data.Entities.Polls;

public class Poll
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string CreatedByUserId { get; set; } = null!;
    
    public EntityType EntityType { get; set; }
    
    public string Question { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    
    public Group Group { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
}