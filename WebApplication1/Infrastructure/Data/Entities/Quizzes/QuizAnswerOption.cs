namespace WebApplication1.Infrastructure.Data.Entities.Quizzes;

public class QuizAnswerOption
{
    public string Id { get; set; } = null!;
    public string QuestionId { get; set; } = null!;
    public string Text { get; set; } = null!;
    public bool IsCorrect { get; set; }

    public QuizQuestion Question { get; set; } = null!;
}