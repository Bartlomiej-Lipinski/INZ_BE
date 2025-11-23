using WebApplication1.Infrastructure.Data.Entities.Groups;

namespace WebApplication1.Infrastructure.Data.Entities.Materials;

public class Material
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string UserId { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public string? Content { get; set; }
    public string? CategoryId { get; set; }

    public DateTime CreatedAt { get; set; }
     
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
    public MaterialCategory? MaterialCategory { get; set; }
}