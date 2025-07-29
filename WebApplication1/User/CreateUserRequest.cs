using System.ComponentModel.DataAnnotations;

namespace WebApplication1.User;

public class CreateUserRequest
{
   public string Name { get; set; }
   public string Surname { get; set; }
   [EmailAddress]
   public string Email { get; set; }
   public DateOnly? BirthDate { get; set; }
   public string Password { get; set; }
}