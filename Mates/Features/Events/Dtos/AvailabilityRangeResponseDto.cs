using Mates.Features.Users.Dtos;

namespace Mates.Features.Events.Dtos;

public record AvailabilityRangeResponseDto
{
    public string Id { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public UserResponseDto User { get; set; } = null!;
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; }
}