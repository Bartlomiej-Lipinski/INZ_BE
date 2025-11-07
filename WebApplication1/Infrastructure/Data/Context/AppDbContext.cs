using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Entities.Settlements;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Data.Entities.Tokens;
using WebApplication1.Infrastructure.Data.Entities.Polls;

namespace WebApplication1.Infrastructure.Data.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupUser> GroupUsers { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<TwoFactorCode> TwoFactorCodes { get; set; }
    public DbSet<Recommendation> Recommendations { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Reaction> Reactions { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventAvailability> EventAvailabilities { get; set; }
    public DbSet<EventAvailabilityRange> EventAvailabilityRanges { get; set; }
    public DbSet<EventSuggestion> EventSuggestions { get; set; }
    public DbSet<StoredFile> StoredFiles { get; set; } = null!;
    public DbSet<Expense> Expenses { get; set; } = null!;
    public DbSet<ExpenseBeneficiary> ExpenseBeneficiaries { get; set; } = null!;
    public DbSet<Settlement> Settlements { get; set; } = null!;
    public DbSet<Poll> Polls { get; set; } = null!;
    public DbSet<PollOption> PollOptions { get; set; } = null!;
     
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.UserId).IsRequired();
            
            entity.HasOne(gu => gu.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(gu => gu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.Color).IsRequired().HasMaxLength(20);
            entity.Property(g => g.Code).IsRequired().HasMaxLength(10);
            
            entity.HasMany(g => g.GroupUsers)
                .WithOne(gu => gu.Group)
                .HasForeignKey(gu => gu.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(g => g.Recommendations)
                .WithOne(r => r.Group)
                .HasForeignKey(r => r.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(g => g.Events)
                .WithOne(e => e.Group)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(g => g.Expenses)
                .WithOne(e => e.Group)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(g => g.Settlements)
                .WithOne(s => s.Group)
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(g => g.Polls)
                .WithOne(p => p.Group)
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(g => g.Code).IsUnique();
        });
        
        builder.Entity<GroupUser>(entity =>
        {
            entity.HasKey(gu => new { gu.UserId, gu.GroupId });
            entity.Property(gu => gu.IsAdmin).IsRequired();
            
            entity.HasOne(gu => gu.Group)
                .WithMany(g => g.GroupUsers)
                .HasForeignKey(gu => gu.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(gu => gu.User)
                .WithMany(u => u.GroupUsers)
                .HasForeignKey(gu => gu.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.TokenHash).IsRequired().HasMaxLength(512);
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.ExpiresAt).IsRequired();
            
            entity.HasOne(t => t.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(t => new { t.UserId, t.IsUsed });
            entity.HasIndex(t => t.ExpiresAt);
        });

        builder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.AttemptTime).IsRequired();
            
            entity.HasIndex(e => new { e.Email, e.IpAddress, e.AttemptTime });
            entity.HasIndex(e => e.AttemptTime);
        });

        builder.Entity<TwoFactorCode>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.UserId).IsRequired();
            entity.Property(t => t.Code).IsRequired().HasMaxLength(6);
            entity.Property(t => t.IpAddress).HasMaxLength(45);
            entity.Property(t => t.UserAgent).HasMaxLength(500);
            
            entity.HasOne(t => t.User)
                .WithMany(u => u.TwoFactorCodes)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(t => new { t.UserId, t.IsUsed, t.ExpiresAt });
            entity.HasIndex(t => t.ExpiresAt);
            entity.HasIndex(t => t.UserId);
        });
        
        builder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.GroupId).IsRequired();
            entity.Property(r => r.UserId).IsRequired();
            entity.Property(r => r.Title).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Content).IsRequired().HasMaxLength(2000);
            entity.Property(r => r.Category).HasMaxLength(100);

            entity.HasOne(r => r.Group)
                .WithMany(g => g.Recommendations)
                .HasForeignKey(r => r.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                .WithMany(u => u.Recommendations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.TargetId).IsRequired();
            entity.Property(c => c.TargetType).IsRequired();
            entity.Property(c => c.UserId).IsRequired();
            entity.Property(c => c.Content).IsRequired().HasMaxLength(1000);
            
            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(c => new { c.TargetType, c.TargetId });
        });
        
        builder.Entity<Reaction>(entity =>
        {
            entity.HasKey(r => new { r.TargetId, r.UserId });
            entity.Property(r => r.TargetId).IsRequired();
            entity.Property(r => r.TargetType).IsRequired();
            entity.Property(r => r.UserId).IsRequired();
            
            entity.HasOne(r => r.User)
                .WithMany(u => u.Reactions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(r => new { r.TargetType, r.TargetId });
        });
        
        builder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GroupId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsAutoScheduled).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Events)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Events)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.GroupId, e.StartDate });
        });

        builder.Entity<EventAvailability>(entity =>
        {
            entity.HasKey(e => new { e.EventId, e.UserId });
            entity.Property(ea => ea.Status).IsRequired();

            entity.HasOne(e => e.Event)
                .WithMany(e => e.Availabilities)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.EventAvailabilities)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<EventAvailabilityRange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.AvailableFrom).IsRequired();
            entity.Property(e => e.AvailableTo).IsRequired();

            entity.HasOne(e => e.Event)
                .WithMany(e => e.AvailabilityRanges)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.EventAvailabilityRanges)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<EventSuggestion>(entity =>
        {
            entity.HasKey(es => es.Id);
            entity.Property(es => es.EventId).IsRequired();
            entity.Property(es => es.StartTime).IsRequired();
            entity.Property(es => es.AvailableUserCount).IsRequired();

            entity.HasOne(es => es.Event)
                .WithMany(e => e.Suggestions)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GroupId).IsRequired();
            entity.Property(e => e.PaidByUserId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsEvenSplit).IsRequired();

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Expenses)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.PaidByUser)
                .WithMany(u => u.Expenses)
                .HasForeignKey(e => e.PaidByUserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Beneficiaries)
                .WithOne(b => b.Expense)
                .HasForeignKey(e => e.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<ExpenseBeneficiary>(entity =>
        {
            entity.HasKey(eb => new { eb.ExpenseId, eb.UserId });
            entity.Property(eb => eb.Share).IsRequired();

            entity.HasOne(eb => eb.Expense)
                .WithMany(e => e.Beneficiaries)
                .HasForeignKey(eb => eb.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(eb => eb.User)
                .WithMany(u => u.ExpenseBeneficiaries)
                .HasForeignKey(eb => eb.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<Settlement>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.GroupId).IsRequired();
            entity.Property(s => s.FromUserId).IsRequired();
            entity.Property(s => s.ToUserId).IsRequired();
            entity.Property(s => s.Amount).IsRequired();

            entity.HasOne(s => s.Group)
                .WithMany(g => g.Settlements)
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(s => s.FromUser)
                .WithMany(u => u.SettlementsTo)
                .HasForeignKey(s => s.FromUserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(s => s.ToUser)
                .WithMany(u => u.SettlementsFrom)
                .HasForeignKey(s => s.ToUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<Poll>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.GroupId).IsRequired();
            entity.Property(p => p.CreatedByUserId).IsRequired();
            entity.Property(p => p.Question).IsRequired().HasMaxLength(500);
            entity.Property(p => p.CreatedAt).IsRequired();

            entity.HasOne(p => p.Group)
                .WithMany(g => g.Polls)
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(p => p.CreatedByUser)
                .WithMany(u => u.Polls)
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasMany(p => p.Options)
                .WithOne(o => o.Poll)
                .HasForeignKey(o => o.PollId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<PollOption>(entity =>
        {
            entity.HasKey(po => po.Id);
            entity.Property(po => po.PollId).IsRequired();
            entity.Property(po => po.Text).IsRequired().HasMaxLength(300);
            
            entity.HasOne(po => po.Poll)
                .WithMany(p => p.Options)
                .HasForeignKey(po => po.PollId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(po => po.VotedUsers)
                .WithMany(u => u.VotedPollOptions)
                .UsingEntity<Dictionary<string, object>>(
                    "UserPollOption",
                    j => j
                        .HasOne<User>()
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<PollOption>()
                        .WithMany()
                        .HasForeignKey("PollOptionId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("UserId", "PollOptionId");
                        j.ToTable("UserPollOptions");
                    });
        });
    }
}