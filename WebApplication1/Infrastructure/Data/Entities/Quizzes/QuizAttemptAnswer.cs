namespace WebApplication1.Infrastructure.Data.Entities.Quizzes;

public class QuizAttemptAnswer
{
    public string Id { get; set; } = null!;
    public string AttemptId { get; set; } = null!;
    public string QuestionId { get; set; } = null!;

    public string? TextAnswer { get; set; }
    public string? SelectedOptionId { get; set; }
    public bool? SelectedTrueFalse { get; set; }
    
    public QuizAttempt QuizAttempt { get; set; } = null!;
    public QuizQuestion QuizQuestion { get; set; } = null!;
}