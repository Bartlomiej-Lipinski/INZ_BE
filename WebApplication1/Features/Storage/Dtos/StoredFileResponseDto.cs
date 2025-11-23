namespace WebApplication1.Features.Storage.Dtos;

public record StoredFileResponseDto
{
    public string Id { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long Size { get; init; }
    public string Url { get; init; } = null!;
    public string EntityType { get; init; } = null!;
    public string? EntityId { get; init; } = null!;
    public FileCategoryResponseDto? FileCategory { get; init; }
    public DateTime UploadedAt { get; init; }
}