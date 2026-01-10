using Mates.Infrastructure.Data.Entities.Events;

namespace Mates.Features.Events.Dtos;

public record EventAvailabilityRequestDto
{
    public EventAvailabilityStatus Status { get; set; }
}