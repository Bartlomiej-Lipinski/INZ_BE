namespace WebApplication1.Infrastructure.Data.Entities.Quizzes;

public class QuizQuestion
{
    public string Id { get; set; } = null!;
    public string QuizId { get; set; } = null!;
    public QuizQuestionType Type { get; set; }
    public string Content { get; set; } = null!;

    public string? CorrectAnswerText { get; set; }
    public bool? CorrectTrueFalse { get; set; }

    public ICollection<QuizAnswerOption> Options { get; set; }
    public Quiz Quiz { get; set; }
}

public enum QuizQuestionType
{
    SingleChoice = 1,
    Open = 2,
    TrueFalse = 3
}