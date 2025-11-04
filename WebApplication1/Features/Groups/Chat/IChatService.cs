namespace WebApplication1.Features.Groups.Chat;

public interface IChatService
{
    Task<ChatMessageDto> SendMessageAsync(Guid groupId, string content, string senderId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ChatMessageDto>> GetRecentMessagesAsync(Guid groupId, int limit = 50,
        CancellationToken cancellationToken = default);
}