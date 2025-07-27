namespace WebApplication1.user.dto;

public class UserResponseDto
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? Photo { get; set; }

}