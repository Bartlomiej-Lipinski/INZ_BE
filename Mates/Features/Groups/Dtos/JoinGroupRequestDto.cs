using System.ComponentModel.DataAnnotations;

namespace Mates.Features.Groups.Dtos;

public record JoinGroupRequestDto
{
    [Required]
    [MaxLength(5)]
    public string GroupCode { get; set; }
}