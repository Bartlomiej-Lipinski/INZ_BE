using System.ComponentModel.DataAnnotations;

namespace Mates.Features.Auth;

public class TwoFactorVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be exactly 6 digits")]
    public string Code { get; set; } = null!;
}