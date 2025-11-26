namespace WebApplication1.Features.Quizzes.Dtos;

public record QuizAttemptRequestDto
{
    public List<QuizAttemptAnswerRequestDto> Answers { get; set; } = [];
}