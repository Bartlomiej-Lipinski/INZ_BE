using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Infrastructure.Data.Entities;

public class GroupUser
{
    [Key]
    public string Id { get; set; } = null!;
    [ForeignKey(nameof(Group))]
    [Column(Order = 0)]
    public string GroupId { get; set; } = null!;
    [ForeignKey(nameof(User))]
    [Column(Order = 1)]
    public string UserId { get; set; } = null!;
    [Required]
    public bool IsAdmin { get; set; }
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}