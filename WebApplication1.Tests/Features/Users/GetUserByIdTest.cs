using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Features.Users;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Users;

public class GetUserByIdTest: TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Ok_When_User_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContextWithUser();
        var mockLogger = new Mock<ILogger<GetUserById>>();
        
        dbContext.Users.Add(TestDataFactory.CreateUser(
            "user1", "Test", "test@test.com", "testUser", "User"));
        await dbContext.SaveChangesAsync();
        var claimsPrincipal = CreateClaimsPrincipal("user1");
        
        var result = await GetUserById.Handle(
            "user1",
            claimsPrincipal,
            dbContext, 
            httpContext, 
            mockLogger.Object, 
            CancellationToken.None
        );

        result.Should().BeOfType<Ok<ApiResponse<GetUserById.UserResponseDto>>>();
        var okResult = result as Ok<ApiResponse<GetUserById.UserResponseDto>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Id.Should().Be("user1");
        okResult.Value.Data.UserName.Should().Be("testUser");
        okResult.Value.Data.Email.Should().Be("test@test.com");
        okResult.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Does_Not_Exist_Or_Is_Not_CurrentUser()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContextWithUser();
        var mockLogger = new Mock<ILogger<GetUserById>>();
        
        var user = TestDataFactory.CreateUser("user1", "Test", "test@test.com", "testUser", "User");
        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        
        var result = await GetUserById.Handle(
            "nonexistent",
            claimsPrincipal,
            dbContext,
            httpContext,
            mockLogger.Object,
            CancellationToken.None
        );

        result.Should().BeOfType<ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Id_Is_NullOrEmpty()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContextWithUser();
        var mockLogger = new Mock<ILogger<GetUserById>>();

        var user = TestDataFactory.CreateUser("user1", "Test", "test@test.com", "testUser", "User");
        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        var result = await GetUserById
            .Handle("",claimsPrincipal ,dbContext, httpContext, mockLogger.Object, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
        var badRequest = result as BadRequest<ApiResponse<string>>;
        badRequest!.Value!.Success.Should().BeFalse();
        badRequest.Value.Message.Should().Be("User ID cannot be null or empty.");
        badRequest.Value.TraceId.Should().Be("test-trace-id");
    }
}