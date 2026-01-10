namespace Mates.Features.Quizzes.Dtos;

public record QuizAnswerOptionResponseDto
{
    public string Id { get; set; } = null!;
    public string QuestionId { get; set; } = null!;
    public string Text { get; set; } = null!;
    public bool? IsCorrect { get; set; }
}