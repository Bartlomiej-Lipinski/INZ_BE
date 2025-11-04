using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;

namespace WebApplication1.Features.Groups.Chat;

public class ChatService : IChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChatMessageDto> SendMessageAsync(Guid groupId, string content, string senderId,
        CancellationToken cancellationToken = default)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Sender = senderId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(message);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetRecentMessagesAsync(Guid groupId, int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var messages = await _db.Messages
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        messages.Reverse(); // od najstarszych do newest

        return messages.Select(MapToDto).ToList();
    }

    private static ChatMessageDto MapToDto(Message m)
    {
        return new ChatMessageDto
        {
            Id = m.Id,
            GroupId = m.GroupId,
            SenderId = m.Sender,
            SenderName = m.Sender, // możesz tu dołączyć tabelę Users, jeśli chcesz imię
            Content = m.Content,
            SentAt = m.SentAt
        };
    }
}