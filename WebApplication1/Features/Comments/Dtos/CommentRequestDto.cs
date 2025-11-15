namespace WebApplication1.Features.Comments.Dtos;

public record CommentRequestDto
{
    public string EntityType { get; set; } = null!;
    public string Content { get; set; } = null!;
}