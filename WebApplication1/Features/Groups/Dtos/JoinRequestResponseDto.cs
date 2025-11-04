namespace WebApplication1.Features.Groups.Dtos;

public record JoinRequestResponseDto
{
    public string GroupId { get; init; } = null!;
    public string GroupName { get; init; }
    public string UserId { get; init; } = null!;
    public string? UserName { get; init; }
}