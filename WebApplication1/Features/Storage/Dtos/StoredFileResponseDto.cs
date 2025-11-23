namespace WebApplication1.Features.Storage.Dtos;

public record StoredFileResponseDto
{
    public string Id { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public string Url { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? EntityId { get; set; }
    public FileCategoryResponseDto? FileCategory { get; set; }
    public DateTime UploadedAt { get; set; }
}