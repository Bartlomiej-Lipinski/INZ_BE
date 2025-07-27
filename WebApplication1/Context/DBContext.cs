using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Auth;

namespace WebApplication1.Context;

public class DBContext : IdentityDbContext<User.User>
{
    public DBContext(DbContextOptions<DBContext> options) : base(options)
    {}

    public DbSet<RefreshToken> RefreshTokens;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
}