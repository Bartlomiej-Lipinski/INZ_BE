namespace WebApplication1.Features.Storage.Dtos;

public record UploadFileRequestDto
{
    public IFormFile File { get; set; } = null!;
    public string? CategoryId { get; set; }
}