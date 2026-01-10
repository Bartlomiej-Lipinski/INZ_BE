namespace Mates.Infrastructure.Data.Entities.Polls;

public class PollOption
{
    public string Id { get; set; } = null!;
    public string PollId { get; set; } = null!;
    public string Text { get; set; } = null!;
    
    public Poll Poll { get; set; } = null!;
    public ICollection<User> VotedUsers { get; set; } = new List<User>();
}