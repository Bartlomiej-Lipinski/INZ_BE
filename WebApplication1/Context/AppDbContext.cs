using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Auth;
using WebApplication1.Models;

namespace WebApplication1.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User.User>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Group.Group> Groups { get; set; }
    public DbSet<GroupUser.GroupUser> GroupUsers { get; set; }
}