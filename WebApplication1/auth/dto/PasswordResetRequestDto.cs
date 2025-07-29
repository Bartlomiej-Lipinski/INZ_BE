using System.ComponentModel.DataAnnotations;

namespace WebApplication1.auth.dto;

public class PasswordResetRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}