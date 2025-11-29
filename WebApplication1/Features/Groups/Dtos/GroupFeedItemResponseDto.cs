using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Features.Groups.Dtos;

public record GroupFeedItemResponseDto
{
    public string Id { get; set; } = null!;
    public FeedItemType Type { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; } = null!;
    public string? StoredFileId { get; set; }
    public string? EntityId { get; set; } 
}