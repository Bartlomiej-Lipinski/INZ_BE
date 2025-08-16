using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;

namespace WebApplication1.Tests.Features;

public class TestBase
{
    protected AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        return new AppDbContext(options);
    }
}