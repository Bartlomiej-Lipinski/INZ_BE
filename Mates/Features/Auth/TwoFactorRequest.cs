using System.ComponentModel.DataAnnotations;

namespace Mates.Features.Auth;

public class TwoFactorRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}