using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Features.Auth;

public class TwoFactorRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}