namespace WebApplication1.Group.dto;

public class GroupResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Color { get; set; } = null!;
    public string Code { get; set; } = null!;
}