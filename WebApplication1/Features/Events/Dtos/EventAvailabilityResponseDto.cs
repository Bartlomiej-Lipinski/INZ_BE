using WebApplication1.Infrastructure.Data.Entities.Events;

namespace WebApplication1.Features.Events.Dtos;

public record EventAvailabilityResponseDto
{
    public string UserId { get; set; } = null!;
    public EventAvailabilityStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}