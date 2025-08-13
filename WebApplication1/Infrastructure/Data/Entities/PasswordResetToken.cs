using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Infrastructure.Data.Entities;

public class PasswordResetToken
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public string TokenHash { get; set; } = null!; //TODO add index on db
    [Required]
    public string UserId { get; set; } = null!;
    [Required]
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public User? User { get; set; }
}