using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Enums;

namespace WebApplication1.Infrastructure.Data.Entities.Storage;

public class StoredFile
{
    public string Id { get; set; } = null!;
    public string? GroupId { get; set; }
    public string UploadedById { get; set; } = null!;
    
    public EntityType EntityType { get; set; }
    public string? EntityId { get; set; } = null!;

    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public string Url { get; set; } = null!;
    public DateTime UploadedAt { get; set; }

    public Group? Group { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}