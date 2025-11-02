using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApplication1.Infrastructure.Data.Context;

namespace WebApplication1.Tests.Features;

public class TestBase
{
    protected static AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }
    
    protected static DefaultHttpContext CreateHttpContext(string? userId = null)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace-id"
        };

        if (string.IsNullOrEmpty(userId)) return context;
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId)
        ], "TestAuth");
        context.User = new ClaimsPrincipal(identity);
        return context;
    }
    
    protected static ClaimsPrincipal CreateClaimsPrincipal(string? userId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId ?? string.Empty)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}