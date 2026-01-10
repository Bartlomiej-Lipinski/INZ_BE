using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Infrastructure.Data.Enums;

namespace Mates.Infrastructure.Data.Entities.Storage;

public class StoredFile
{
    public string Id { get; set; } = null!;
    public string? GroupId { get; set; }
    public string UploadedById { get; set; } = null!;
    
    public EntityType EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? CategoryId { get; set; }

    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public string Url { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
    
    public Group? Group { get; set; }
    public User UploadedBy { get; set; } = null!;
    public FileCategory? FileCategory { get; set; }
}