using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Features.Groups.Dtos;

public record AcceptUserJoinRequestDto
{
    [Required]
    [MaxLength(50)]
    public string GroupId { get; init; } = null!;
    [Required]
    [MaxLength(50)]
    public string UserId { get; init; } = null!;
}