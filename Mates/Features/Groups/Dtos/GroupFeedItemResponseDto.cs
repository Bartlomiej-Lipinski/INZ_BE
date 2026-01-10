using Mates.Features.Comments.Dtos;
using Mates.Features.Users.Dtos;
using Mates.Infrastructure.Data.Entities;

namespace Mates.Features.Groups.Dtos;

public record GroupFeedItemResponseDto
{
    public string Id { get; set; } = null!;
    public FeedItemType Type { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserResponseDto User { get; set; } = null!;
    public string? StoredFileId { get; set; }
    public string? EntityId { get; set; } 
    public List<CommentResponseDto> Comments { get; set; } = [];
    public List<UserResponseDto> Reactions { get; set; } = [];
}