namespace WebApplication1.Features.Storage.Dtos;

public record ProfilePictureResponseDto
{
    public string FileName { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long Size { get; init; }
    public string Url { get; init; } = null!;
}