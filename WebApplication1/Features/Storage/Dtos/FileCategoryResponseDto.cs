namespace WebApplication1.Features.Storage.Dtos;

public record FileCategoryResponseDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
}