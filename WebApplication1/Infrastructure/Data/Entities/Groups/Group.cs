using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Infrastructure.Data.Entities.Settlements;

namespace WebApplication1.Infrastructure.Data.Entities.Groups;

[Index(nameof(Code), IsUnique = true)]
public class Group
{
    [Key]
    public string Id { get; init; } = null!;
    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public string Color { get; set; } = null!;
    [Required]
    public string Code { get; set; } = null!;
    public DateTime? CodeExpirationTime { get; set; } 
    
    public ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}