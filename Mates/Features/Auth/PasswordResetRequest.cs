using System.ComponentModel.DataAnnotations;

namespace Mates.Features.Auth;

public class PasswordResetRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}