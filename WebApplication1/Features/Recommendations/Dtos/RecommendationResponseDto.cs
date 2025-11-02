using WebApplication1.Features.Comments.Dtos;

namespace WebApplication1.Features.Recommendations.Dtos;

public record RecommendationResponseDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; } = null!;
    public List<CommentResponseDto> Comments { get; set; } = [];
    public List<ReactionDto> Reactions { get; set; } = [];
}