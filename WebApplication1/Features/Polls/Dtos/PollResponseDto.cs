using WebApplication1.Features.Users.Dtos;

namespace WebApplication1.Features.Polls.Dtos;

public record PollResponseDto
{
    public string Id { get; set; } = null!;
    public UserResponseDto CreatedByUser { get; set; } = null!;
    public string Question { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<PollOptionDto> Options { get; set; } = [];
}