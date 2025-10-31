namespace WebApplication1.Infrastructure.Data.Entities.Tokens;

public class RefreshToken
{
    public string Id { get; init; } = null!;
    
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    
    public User User { get; set; } = null!;
}