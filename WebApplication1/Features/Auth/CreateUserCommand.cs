namespace WebApplication1.Features.Auth;

public class CreateUserCommand
{
    public string Name { get; set; }
    public string UserName { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    public DateOnly? BirthDate { get; set; }

    public string Password { get; set; }
}
