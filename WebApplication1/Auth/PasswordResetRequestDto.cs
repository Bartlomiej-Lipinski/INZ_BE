using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Auth;

public class PasswordResetRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}