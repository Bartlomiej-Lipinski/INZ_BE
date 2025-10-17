using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Entities.Recommendations;

namespace WebApplication1.Infrastructure.Data.Entities.Groups;

[Index(nameof(Code), IsUnique = true)]
public class Group
{
    [Key]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Color { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;
    
    public DateTime? CodeExpirationTime { get; set; } 
    
    public ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
    
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
}