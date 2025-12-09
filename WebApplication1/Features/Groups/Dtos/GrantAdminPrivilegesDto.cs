namespace WebApplication1.Features.Groups.Dtos;

public record GrantAdminPrivilegesDto
{
    public string UserId { get; set; } = null!;
}