namespace WebApplication1.Infrastructure.Data.Entities.Groups;

public class GroupUser
{
    public string Id { get; init; } = null!;
    
    public string GroupId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public bool IsAdmin { get; set; }
    public AcceptanceStatus AcceptanceStatus { get; set; } 
    
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
public enum AcceptanceStatus
{
    Pending,
    Accepted,
    Rejected
}