using Microsoft.AspNetCore.Identity;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Entities.Polls;
using WebApplication1.Infrastructure.Data.Entities.Settlements;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Data.Entities.Tokens;

namespace WebApplication1.Infrastructure.Data.Entities;

public class User : IdentityUser
{
    /*
     * id provided by IdentityUser
     * email provided by IdentityUser
     * nickname provided by IdentityUser as UserName
     * passwordHash provided by IdentityUser
     * paswordSalt provided by IdentityUser as SecurityStamp
     *
     */
    public string? Name { get; set; }

    public string? Surname { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Status { get; set; }

    public string? Description { get; set; }

    public string? Photo { get; set; }
    
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<EventAvailability> EventAvailabilities { get; set; } = new List<EventAvailability>();
    public ICollection<EventAvailabilityRange> EventAvailabilityRanges { get; set; } = new List<EventAvailabilityRange>();
    public ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
    public ICollection<Poll> Polls { get; set; } = new List<Poll>();
    public ICollection<PollOption> VotedPollOptions { get; set; } = new List<PollOption>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<ExpenseBeneficiary> ExpenseBeneficiaries { get; set; } = new List<ExpenseBeneficiary>();
    public ICollection<Settlement> SettlementsFrom { get; set; } = new List<Settlement>();
    public ICollection<Settlement> SettlementsTo { get; set; } = new List<Settlement>();
    public ICollection<StoredFile> Files { get; set; } = new List<StoredFile>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public ICollection<TwoFactorCode> TwoFactorCodes { get; set; } = new List<TwoFactorCode>();
    public ICollection<TimelineCustomEvent> TimelineCustomEvents { get; set; } = new List<TimelineCustomEvent>();
}