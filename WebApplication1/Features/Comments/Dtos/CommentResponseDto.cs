namespace WebApplication1.Features.Comments.Dtos;

public record CommentResponseDto
{
    public string Id { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}