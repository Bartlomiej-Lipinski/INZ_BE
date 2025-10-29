namespace WebApplication1.Features.Groups.Dtos;

public record GrantAdminPrivilegesDto
{
    public string GroupId { get; set; } = null!;
    public string UserId { get; set; } = null!;
}