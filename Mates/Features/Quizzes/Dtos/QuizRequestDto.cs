namespace Mates.Features.Quizzes.Dtos;

public record QuizRequestDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public List<QuizQuestionRequestDto> Questions { get; set; } = [];
}