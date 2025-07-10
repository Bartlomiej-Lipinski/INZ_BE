using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebApplication1.Group;

public class Group
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("name")]
    [Required]
    public string? Name { get; set; }
    [Column("color")]
    [Required]
    public string? Color { get; set; }
    [Column("code")]
    [Required]
    public string? Code { get; set; }
}