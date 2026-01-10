using System.ComponentModel.DataAnnotations;

namespace Mates.Features.Auth;

public class ResetPasswordResponse
{
    [Required]
    public string Token { get; set; } = null!;
    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = null!;
}