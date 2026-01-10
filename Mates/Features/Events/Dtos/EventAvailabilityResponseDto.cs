using Mates.Features.Users.Dtos;
using Mates.Infrastructure.Data.Entities.Events;

namespace Mates.Features.Events.Dtos;

public record EventAvailabilityResponseDto
{
    public UserResponseDto User { get; set; } = null!;
    public EventAvailabilityStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}