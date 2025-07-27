using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Group.dto;

public class GroupRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Color { get; set; } = null!;
}