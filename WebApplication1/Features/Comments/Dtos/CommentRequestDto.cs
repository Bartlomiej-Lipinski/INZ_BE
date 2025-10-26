namespace WebApplication1.Features.Comments.Dtos;

public record CommentRequestDto
{
    public string TargetType { get; set; } = null!;
    public string Content { get; set; } = null!;
}