using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Auth;

public class PasswordResetToken
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public Guid UserId { get; init; }

    [Required]
    public string TokenHash { get; init; } = null!;

    [Required]
    public DateTime ExpiresAt { get; init; }

    public bool Used { get; set; } = false;

    public User.User User { get; init; } = null!;
}