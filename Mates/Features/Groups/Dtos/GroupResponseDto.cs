using System.ComponentModel.DataAnnotations;

namespace Mates.Features.Groups.Dtos;

public record GroupResponseDto
{
    [MaxLength(50)]
    public string Id { get; set; } = null!;
    [MaxLength(50)]
    public string Name { get; set; } = null!;
    [MaxLength(7)]
    public string? Color { get; set; }
    [MaxLength(5)]
    public string? Code { get; set; }
}