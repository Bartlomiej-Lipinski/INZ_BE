namespace WebApplication1.Features.Groups.Dtos;

public record JoinRequestResponseDto(string GuGroupId, string GroupName, string GuUserId, string? UserUserName)
{
    public string GroupId { get; init; } = null!;
    public string GroupName { get; init; }
    public string UserId { get; init; } = null!;
    public string? UserName { get; init; }
}