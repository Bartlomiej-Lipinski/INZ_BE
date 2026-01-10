namespace Mates.Features.Quizzes.Dtos;

public record QuizAnswerOptionRequestDto
{
    public string Text { get; set; } = null!;
    public bool IsCorrect { get; set; }
}