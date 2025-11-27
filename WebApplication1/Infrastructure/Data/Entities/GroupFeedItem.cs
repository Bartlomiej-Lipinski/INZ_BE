using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Entities.Storage;

namespace WebApplication1.Infrastructure.Data.Entities;

public class GroupFeedItem
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    
    public FeedItemType Type { get; set; }
    public string? Text { get; set; } 
    public string? StoredFileId { get; set; }
    
    public string? EntityId { get; set; }
    public string UserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
    public StoredFile? StoredFile { get; set; }
}

public enum FeedItemType
{
    Post,
    Event,
    Challenge,
    Poll,
    Expense,
    Quiz,
    Recommendation,
    Other
}