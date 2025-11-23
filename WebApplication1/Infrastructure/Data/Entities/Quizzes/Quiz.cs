using WebApplication1.Infrastructure.Data.Entities.Groups;

namespace WebApplication1.Infrastructure.Data.Entities.Quizzes;

public class Quiz
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<QuizQuestion> Questions { get; set; }
    public ICollection<QuizAttempt> Attempts { get; set; }
}