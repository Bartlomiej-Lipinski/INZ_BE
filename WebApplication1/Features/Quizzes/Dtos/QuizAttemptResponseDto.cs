namespace WebApplication1.Features.Quizzes.Dtos;

public record QuizAttemptResponseDto
{
    public string AttemptId { get; set; } = null!;
    public string QuizId { get; set; } = null!;
    public int Score { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<QuizAttemptAnswerResponseDto> Answers { get; set; } = [];
}