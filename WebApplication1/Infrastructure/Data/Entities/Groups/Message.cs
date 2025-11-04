namespace WebApplication1.Infrastructure.Data.Entities.Groups;

public class Message
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Group? Group { get; set; }
}