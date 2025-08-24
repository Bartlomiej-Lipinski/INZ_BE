using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Users;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Users;

public class DeleteUserTest: TestBase
{
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_UserName_Is_Empty()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var result = await DeleteUser.Handle("", dbContext, CancellationToken.None);
        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var result = await DeleteUser.Handle("missingUser", dbContext, CancellationToken.None);
        result.Should().BeOfType<NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Ok_When_User_Deleted_Successfully()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var dbContext = GetInMemoryDbContext(dbName))
        {
            dbContext.Users.Add(TestDataFactory.CreateUser("u1", "Test User", "test@test.com", "testUser"));
            await dbContext.SaveChangesAsync();
        }

        await using (var context2 = GetInMemoryDbContext(dbName))
        {
            var users = await context2.Users.ToListAsync();
            users.Should().Contain(u => u.UserName == "testUser");

            var result = await DeleteUser.Handle("testUser", context2, CancellationToken.None);

            result.Should().BeOfType<Ok<ApiResponse<string>>>();
            (result as Ok<ApiResponse<string>>)?.Value?.Data.Should().Be("User deleted successfully.");
        }
    }
}