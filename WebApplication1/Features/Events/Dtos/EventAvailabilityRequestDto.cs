using WebApplication1.Infrastructure.Data.Entities.Events;

namespace WebApplication1.Features.Events.Dtos;

public record EventAvailabilityRequestDto
{
    public EventAvailabilityStatus Status { get; set; }
}