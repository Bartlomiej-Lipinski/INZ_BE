using Mates.Infrastructure.Data.Entities.Challenges;
using Mates.Infrastructure.Data.Entities.Comments;
using Mates.Infrastructure.Data.Entities.Events;
using Mates.Infrastructure.Data.Entities.Polls;
using Mates.Infrastructure.Data.Entities.Quizzes;
using Mates.Infrastructure.Data.Entities.Settlements;
using Mates.Infrastructure.Data.Entities.Storage;

namespace Mates.Infrastructure.Data.Entities.Groups;

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
    public ICollection<TimelineEvent> TimelineEvents { get; set; } = new List<TimelineEvent>();
    public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
    public ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    public ICollection<FileCategory> FileCategories { get; set; } = new List<FileCategory>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<GroupFeedItem> GroupFeedItems { get; set; } = new List<GroupFeedItem>();
}