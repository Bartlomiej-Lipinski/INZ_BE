namespace WebApplication1.Features.Groups.Chat;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}