namespace WebApplication1.Infrastructure.Data.Entities.Storage;

public class StoredFile
{
    public string Id { get; set; } = null!;
        
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public string Url { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string UploadedById { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
        
    private User UploadedBy { get; set; } = null!;
}