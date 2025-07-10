using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.GroupUser;

public class GroupUser
{
    [Key]
    [ForeignKey("Group")]
    public Guid GroupId { get; set; }
    [Key]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    [Column("admin")]
    [Required]
    public bool IsAdmin { get; set; }
}