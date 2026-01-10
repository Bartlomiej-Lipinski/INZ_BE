namespace Mates.Features.Quizzes.Dtos;

public record QuizResponseDto
{
    public string Id { get; set; }
    public string GroupId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<QuizQuestionResponseDto> Questions { get; set; }
}