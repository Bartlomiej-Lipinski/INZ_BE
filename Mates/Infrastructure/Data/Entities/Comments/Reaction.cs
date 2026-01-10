using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Infrastructure.Data.Enums;

namespace Mates.Infrastructure.Data.Entities.Comments;

public class Reaction
{
    public string GroupId { get; set; } = null!;
    public string TargetId { get; set; } = null!;
    
    public EntityType EntityType { get; set; }
    
    public string UserId { get; set; } = null!;
    
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}