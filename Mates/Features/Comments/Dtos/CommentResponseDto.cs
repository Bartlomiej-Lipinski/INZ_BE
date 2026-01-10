using Mates.Features.Users.Dtos;

namespace Mates.Features.Comments.Dtos;

public record CommentResponseDto
{
    public string Id { get; set; } = null!;
    public string Content { get; set; } = null!;
    public UserResponseDto User { get; set; } = null!;
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}