namespace WebApplication1.Features.Quizzes.Dtos;

public record QuizAnswerOptionResponseDto
{
    public string Id { get; set; }
    public string Text { get; set; }
    public bool IsCorrect { get; set; }
}