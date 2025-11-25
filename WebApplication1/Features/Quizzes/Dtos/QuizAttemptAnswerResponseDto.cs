namespace WebApplication1.Features.Quizzes.Dtos;

public record QuizAttemptAnswerResponseDto
{
    public string QuestionId { get; set; } = null!;
    public string? SelectedOptionId { get; set; }
    public bool? SelectedTrueFalse { get; set; }
}