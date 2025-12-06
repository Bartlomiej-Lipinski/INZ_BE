using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Entities.Events;

namespace WebApplication1.Features.Events.Dtos;

public record EventAvailabilityResponseDto
{
    public UserResponseDto User { get; set; } = null!;
    public EventAvailabilityStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}