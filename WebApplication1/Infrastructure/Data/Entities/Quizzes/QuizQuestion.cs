namespace WebApplication1.Infrastructure.Data.Entities.Quizzes;

public class QuizQuestion
{
    public string Id { get; set; } = null!;
    public string QuizId { get; set; } = null!;
    public QuizQuestionType Type { get; set; }
    public string Content { get; set; } = null!;

    public bool? CorrectTrueFalse { get; set; }

    public ICollection<QuizAnswerOption> Options { get; set; } = new List<QuizAnswerOption>();
    public Quiz Quiz { get; set; } = null!;
}

public enum QuizQuestionType
{
    SingleChoice = 1,
    TrueFalse = 2
}