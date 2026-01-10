namespace Mates.Features.Groups.Dtos;

public record GroupFeedItemRequestDto
{
    public string? Description { get; set; } 
    public string? Title { get; set; }
    public IFormFile? File { get; set; }
}