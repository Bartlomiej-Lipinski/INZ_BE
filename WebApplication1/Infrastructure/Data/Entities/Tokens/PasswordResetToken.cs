namespace WebApplication1.Infrastructure.Data.Entities.Tokens;

public class PasswordResetToken
{
    public string Id { get; init; } = null!;
    
    public string TokenHash { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    
    public User? User { get; set; }
}