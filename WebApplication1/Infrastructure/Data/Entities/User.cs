using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Infrastructure.Data.Entities;

public class User : IdentityUser
{
    /*
     * id provided by IdentityUser
     * email provided by IdentityUser
     * nickname provided by IdentityUser as UserName
     * passwordHash provided by IdentityUser
     * paswordSalt provided by IdentityUser as SecurityStamp
     *
     */
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? Photo { get; set; }

    public ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
}