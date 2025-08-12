using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Infrastructure.Data.Entities;

public class RefreshToken
{
    [Key] public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}