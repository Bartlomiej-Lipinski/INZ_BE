using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Users;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Users;

public class GetUserByIdTest: TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Ok_When_User_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        dbContext.Users.Add(TestDataFactory.CreateUser(
            "user1", "Test","User", "test@test.com", "testUser"));
        await dbContext.SaveChangesAsync();
        
        var result = await GetUserById.Handle(
            "user1",
            CreateClaimsPrincipal("user1"),
            dbContext, 
            CreateHttpContext(), 
            NullLogger<GetUserById>.Instance, 
            CancellationToken.None
        );

        result.Should().BeOfType<Ok<ApiResponse<UserResponseDto>>>();
        var okResult = result as Ok<ApiResponse<UserResponseDto>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Id.Should().Be("user1");
        okResult.Value.Data.Username.Should().Be("testUser");
        okResult.Value.Data.Email.Should().Be("test@test.com");
        okResult.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Does_Not_Exist_Or_Is_Not_CurrentUser()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var user = TestDataFactory.CreateUser("user1", "Test","User", "test@test.com", "testUser");
        
        var result = await GetUserById.Handle(
            "nonexistent",
            CreateClaimsPrincipal(user.Id),
            dbContext,
            CreateHttpContext(),
            NullLogger<GetUserById>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<ForbidHttpResult>();
    }
}