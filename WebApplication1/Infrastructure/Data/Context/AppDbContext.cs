using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Data.Entities.Tokens;

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
     
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.HasIndex(e => new { e.Email, e.IpAddress, e.AttemptTime });
            entity.HasIndex(e => e.AttemptTime);
        });

        builder.Entity<TwoFactorCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(6);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasIndex(e => new { e.UserId, e.IsUsed, e.ExpiresAt });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.UserId);
        });
        
        builder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Title).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Content).IsRequired().HasMaxLength(2000);
            entity.Property(r => r.Category).HasMaxLength(100);

            entity.HasOne(r => r.Group)
                .WithMany(g => g.Recommendations)
                .HasForeignKey(r => r.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired().HasMaxLength(1000);
            entity.HasIndex(c => new { c.TargetType, c.TargetId });
            
            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<Reaction>(entity =>
        {
            entity.HasKey(r => new { r.TargetId, r.UserId });
            entity.HasIndex(c => new { c.TargetType, c.TargetId });

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
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
                .WithMany()
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
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<EventAvailabilityRange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AvailableFrom).IsRequired();
            entity.Property(e => e.AvailableTo).IsRequired();

            entity.HasOne(e => e.Event)
                .WithMany(e => e.AvailabilityRanges)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<EventSuggestion>(entity =>
        {
            entity.HasKey(es => es.Id);
            entity.Property(es => es.StartTime).IsRequired();
            entity.Property(es => es.AvailableUserCount).IsRequired();

            entity.HasOne(es => es.Event)
                .WithMany(e => e.Suggestions)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}