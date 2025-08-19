using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Features.Auth;

public class PasswordResetRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}