namespace WebApplication1.Features.Recommendations.Dtos;

public record RecommendationRequestDto
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    
    public IFormFile? File { get; set; }
}