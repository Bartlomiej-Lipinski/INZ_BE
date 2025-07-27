using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WebApplication1.group_user;

namespace WebApplication1.group;

[Index(nameof(Code), IsUnique = true)]
public class Group
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Color { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;

    public ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
}