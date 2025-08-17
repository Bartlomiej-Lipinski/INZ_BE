using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Features.Auth;

public class TwoFactorVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be exactly 6 digits")]
    public string Code { get; set; }
}