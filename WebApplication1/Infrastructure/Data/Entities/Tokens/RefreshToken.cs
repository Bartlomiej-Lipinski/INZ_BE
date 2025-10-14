using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Infrastructure.Data.Entities.Tokens;

public class RefreshToken
{
    [Key]
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    public string Token { get; set; } = string.Empty;
    
    public string UserId { get; set; } = null!;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsRevoked { get; set; }
}