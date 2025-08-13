namespace WebApplication1.Features.Auth;

public class CreateUserCommand
{
    public string Name { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateOnly? BirthDate { get; set; }
    public string Password { get; set; } = null!;
}