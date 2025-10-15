using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebApplication1.Features.Users;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Users;

public class DeleteUserTest: TestBase
{
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_UserId_Is_Empty()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = NullLogger<DeleteUser>.Instance;
        var claimsPrincipal = CreateClaimsPrincipal();

        var result = await DeleteUser
            .Handle(claimsPrincipal, dbContext, httpContext, mockLogger, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
        var badRequest = result as BadRequest<ApiResponse<string>>;
        badRequest!.Value!.TraceId.Should().Be("test-trace-id");
        badRequest.Value.Message.Should().Be("User ID cannot be null or empty.");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<DeleteUser>>();
        
        var user1 = TestDataFactory.CreateUser("u1", "Test User", "test@test.com", "testUser");
        var claimsPrincipal = CreateClaimsPrincipal(user1.Id);
        var result = await DeleteUser
            .Handle(claimsPrincipal, dbContext, httpContext, mockLogger.Object, CancellationToken.None);

        result.Should().BeOfType<NotFound<ApiResponse<string>>>();
        var notFound = result as NotFound<ApiResponse<string>>;
        notFound!.Value!.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_Ok_When_User_Deleted_Successfully()
    {
        var dbName = Guid.NewGuid().ToString();
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<DeleteUser>>();
        
        await using (var dbContext = GetInMemoryDbContext(dbName))
        {
            dbContext.Users.Add(TestDataFactory.CreateUser("u1", "Test User", "test@test.com", "testUser"));
            await dbContext.SaveChangesAsync();
        }

        await using (var context2 = GetInMemoryDbContext(dbName))
        {
            var users = await context2.Users.ToListAsync();
            users.Should().Contain(u => u.UserName == "testUser");

            var claimsPrincipal = CreateClaimsPrincipal("u1");

            var result = await DeleteUser.Handle(
                claimsPrincipal, 
                context2, 
                httpContext, 
                mockLogger.Object,
                CancellationToken.None
            );

            result.Should().BeOfType<Ok<ApiResponse<string>>>();
            var okResult = result as Ok<ApiResponse<string>>;
            okResult!.Value!.Data.Should().Be("User deleted successfully.");
            okResult.Value.TraceId.Should().Be("test-trace-id");
        }
    }
}