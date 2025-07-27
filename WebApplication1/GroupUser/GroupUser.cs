using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication1.Models;

namespace WebApplication1.GroupUser;

public class GroupUser
{
    [Key, Column(Order = 0)]
    public Guid GroupId { get; set; }

    [Key, Column(Order = 1)]
    public Guid UserId { get; set; }

    [Required]
    public bool IsAdmin { get; set; }
    
    public Group.Group Group { get; set; } = null!;
    public User.User User { get; set; } = null!;
}