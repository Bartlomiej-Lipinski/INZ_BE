using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class UserDBContext : IdentityDbContext<User>
{
    
}