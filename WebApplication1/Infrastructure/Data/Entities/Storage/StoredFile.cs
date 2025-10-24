namespace WebApplication1.Infrastructure.Data.Entities.Storage
{
    public class StoredFile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long Size { get; set; }
        public string Url { get; set; } = null!;
        public string EntityType { get; set; } = null!; // e.g. "User", "Event"
        public string EntityId { get; set; } = null!;
        public string UploadedBy { get; set; } = null!;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}