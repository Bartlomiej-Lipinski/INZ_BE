using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Auth;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }
    public string Token { get; set; } 
    public string UserId { get; set; } 
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}