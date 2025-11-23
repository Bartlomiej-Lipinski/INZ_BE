using WebApplication1.Infrastructure.Data.Entities.Groups;

namespace WebApplication1.Infrastructure.Data.Entities.Materials;

public class MaterialCategory
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string Name { get; set; } = null!;
    
    public Group Group { get; set; } = null!;
}