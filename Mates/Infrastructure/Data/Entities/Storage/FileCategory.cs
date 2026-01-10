using Mates.Infrastructure.Data.Entities.Groups;

namespace Mates.Infrastructure.Data.Entities.Storage;

public class FileCategory
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string Name { get; set; } = null!;
    
    public Group Group { get; set; } = null!;
}