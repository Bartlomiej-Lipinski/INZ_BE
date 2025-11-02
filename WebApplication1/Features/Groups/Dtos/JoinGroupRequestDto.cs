using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Features.Groups.Dtos;

public record JoinGroupRequestDto
{
    [Required]
    [MaxLength(5)]
    public string GroupCode { get; set; }
}