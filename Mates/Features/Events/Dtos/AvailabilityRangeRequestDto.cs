namespace Mates.Features.Events.Dtos;

public record AvailabilityRangeRequestDto
{
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; }
}