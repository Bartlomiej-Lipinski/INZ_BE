using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApplication1.Features.Groups.Chat;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        // opcjonalnie: logowanie połączenia
        _logger.LogInformation("Connection {ConnectionId} connected", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGroup(Guid groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());

        var history = await _chatService.GetRecentMessagesAsync(groupId);
        await Clients.Caller.SendAsync("ReceiveMessageHistory", history);
    }

    public async Task LeaveGroup(Guid groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
    }

    public async Task SendMessage(Guid groupId, string content, CancellationToken cancellationToken = default)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }

        // Zakładamy, że IChatService tworzy i zwraca obiekt wiadomości (ChatMessageDto)
        var message = await _chatService.SendMessageAsync(groupId, content, userId, cancellationToken);

        // Odeślij wiadomość do wszystkich w grupie
        await Clients.Group(groupId.ToString())
            .SendAsync("ReceiveMessage", message, cancellationToken);
    }
}