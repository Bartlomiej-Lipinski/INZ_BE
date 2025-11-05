using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Infrastructure.Data.Entities.Settlements;
using WebApplication1.Infrastructure.Data.Entities.Polls;

namespace WebApplication1.Infrastructure.Data.Entities.Groups;

public class Group
{
    public string Id { get; init; } = null!;

    public string Name { get; set; } = null!;
    public string Color { get; set; } = null!;
    public string Code { get; set; } = null!;
    public DateTime? CodeExpirationTime { get; set; } 
    
    public ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
    public ICollection<Poll> Polls { get; set; } = new List<Poll>();
    public ICollection<TimelineCustomEvent> TimelineCustomEvents { get; set; } = new List<TimelineCustomEvent>();
}