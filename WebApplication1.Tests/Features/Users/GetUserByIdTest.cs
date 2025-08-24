using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using WebApplication1.Features.Users;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Users;

public class GetUserByIdTest: TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Ok_When_User_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        dbContext.Users.Add(TestDataFactory.CreateUser(
            "user1", "Test", "test@test.com", "testUser", "User"));
        await dbContext.SaveChangesAsync();

        var result = await GetUserById.Handle("user1", dbContext, CancellationToken.None);

        result.Should().BeOfType<Ok<ApiResponse<GetUserById.UserResponseDto>>>();
        var okResult = result as Ok<ApiResponse<GetUserById.UserResponseDto>>;
        okResult!.Value!.Data!.Id.Should().Be("user1");
        okResult.Value.Data.UserName.Should().Be("testUser");
        okResult.Value.Data.Email.Should().Be("test@test.com");
    }
    
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await GetUserById.Handle("nonexistent", dbContext, CancellationToken.None);

        result.Should().BeOfType<NotFound<ApiResponse<string>>>();
        var notFound = result as NotFound<ApiResponse<string>>;
        notFound!.Value!.Message.Should().Be("User not found.");
    }
    
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Id_Is_NullOrEmpty()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await GetUserById.Handle("", dbContext, CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
        var badRequest = result as BadRequest<ApiResponse<string>>;
        badRequest!.Value!.Message.Should().Be("User ID cannot be null or empty.");
    }
}