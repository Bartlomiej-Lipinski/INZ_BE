namespace WebApplication1.Features.Groups.Dtos;

public record JoinRequestResponseDto(string GroupId, string GroupName, string UserId, string? UserName);