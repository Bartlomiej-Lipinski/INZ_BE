using System.ComponentModel.DataAnnotations;

namespace WebApplication1.user.dto;

public class UserRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string UserName { get; set; } = null!;

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? Photo { get; set; }
    
    public string? Password { get; set; }
}