namespace WebApplication1.Features.Groups.Dtos;

public record GroupFeedItemRequestDto
{
    public string? Description { get; set; } 
    public IFormFile? File { get; set; }
}