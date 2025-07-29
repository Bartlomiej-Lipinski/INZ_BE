using System.ComponentModel.DataAnnotations;

namespace WebApplication1.User;

public class GetUserResponseModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    [EmailAddress]
    public string Email { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string PasswordHash { get; set; }
    
}