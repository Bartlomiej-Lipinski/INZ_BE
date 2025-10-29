using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Features.Groups.Dtos;

public record RejectUserJoinRequestDto
{
    [Required]
    [MaxLength(50)]
    public string UserId { get; set; } 
}