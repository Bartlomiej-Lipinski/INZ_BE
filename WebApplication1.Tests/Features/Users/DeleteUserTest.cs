using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Features.Users;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Users;

public class DeleteUserTest: TestBase
{
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_UserName_Is_Empty()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockHttpContext = new Mock<HttpContext>();
        var mockLogger = new Mock<ILogger<DeleteUser>>();
        
        mockHttpContext.Setup(x => x.TraceIdentifier).Returns("test-trace-id");
        
        var result = await DeleteUser.Handle("", dbContext, CancellationToken.None,
            mockHttpContext.Object, mockLogger.Object);
            
        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
        var badRequest = result as BadRequest<ApiResponse<string>>;
        badRequest!.Value!.TraceId.Should().Be("test-trace-id");
    }
    
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockHttpContext = new Mock<HttpContext>();
        var mockLogger = new Mock<ILogger<DeleteUser>>();
        
        mockHttpContext.Setup(x => x.TraceIdentifier).Returns("test-trace-id");
        
        var result = await DeleteUser.Handle("missingUser", dbContext, CancellationToken.None,
            mockHttpContext.Object, mockLogger.Object);
            
        result.Should().BeOfType<NotFound<ApiResponse<string>>>();
        var notFound = result as NotFound<ApiResponse<string>>;
        notFound!.Value!.TraceId.Should().Be("test-trace-id");
    }
    
    [Fact]
    public async Task Handle_Should_Return_Ok_When_User_Deleted_Successfully()
    {
        var dbName = Guid.NewGuid().ToString();
        var mockHttpContext = new Mock<HttpContext>();
        var mockLogger = new Mock<ILogger<DeleteUser>>();
        
        mockHttpContext.Setup(x => x.TraceIdentifier).Returns("test-trace-id");

        await using (var dbContext = GetInMemoryDbContext(dbName))
        {
            dbContext.Users.Add(TestDataFactory.CreateUser("u1", "Test User", "test@test.com", "testUser"));
            await dbContext.SaveChangesAsync();
        }

        await using (var context2 = GetInMemoryDbContext(dbName))
        {
            var users = await context2.Users.ToListAsync();
            users.Should().Contain(u => u.UserName == "testUser");

            var result = await DeleteUser.Handle("testUser", context2, CancellationToken.None,
                mockHttpContext.Object, mockLogger.Object);

            result.Should().BeOfType<Ok<ApiResponse<string>>>();
            var okResult = result as Ok<ApiResponse<string>>;
            okResult!.Value!.Message.Should().Be("User deleted successfully.");
            okResult.Value.TraceId.Should().Be("test-trace-id");
        }
    }
}