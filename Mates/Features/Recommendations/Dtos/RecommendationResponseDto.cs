using Mates.Features.Comments.Dtos;
using Mates.Features.Users.Dtos;

namespace Mates.Features.Recommendations.Dtos;

public record RecommendationResponseDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? Category { get; set; }
    public string? LinkUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserResponseDto User { get; set; } = null!;
    public string? StoredFileId { get; set; }
    public List<CommentResponseDto> Comments { get; set; } = [];
    public List<UserResponseDto> Reactions { get; set; } = [];
}