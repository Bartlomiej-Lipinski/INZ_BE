namespace Mates.Infrastructure.Data.Entities;

public class TwoFactorCode
{
    public int Id { get; init; }
        
    public string UserId { get; set; }
    public string Code { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    public User User { get; set; } = null!;
}