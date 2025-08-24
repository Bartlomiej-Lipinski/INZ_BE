using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;

namespace WebApplication1.Tests.Features;

public class TestBase
{
    protected static AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new AppDbContext(options);
    }
}