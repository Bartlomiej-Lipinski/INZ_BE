namespace WebApplication1.Infrastructure.Data.Entities.Quizzes;

public class QuizAttempt
{
    public string Id { get; set; } = null!;
    public string QuizId { get; set; } = null!;
    public string UserId { get; set; }
    public DateTime CompletedAt { get; set; }
    public int Score { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<QuizAttemptAnswer> Answers { get; set; }
}