namespace Mates.Features.Quizzes.Dtos;

public record QuizAttemptAnswerRequestDto
{
    public string QuestionId { get; set; } = null!;
    public string? SelectedOptionId { get; set; }
    public bool? SelectedTrueFalse { get; set; }
}