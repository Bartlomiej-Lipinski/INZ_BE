using FluentAssertions;
using WebApplication1.Features.Users;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Users;

public class DeleteUserTest : TestBase
{
    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenUserNameIsEmpty()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();

        // Act
        var result = await DeleteUser.Handle("", dbContext, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value?.Success.Should().BeFalse();
        badRequest.Value?.Message.Should().Be("User name cannot be null or empty.");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();

        // Act
        var result = await DeleteUser.Handle("nonexistent", dbContext, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value?.Success.Should().BeFalse();
        notFound.Value?.Message.Should().Be("User not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenUserIsDeleted()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await DeleteUser.Handle("testuser", dbContext, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Message.Should().Be("User deleted successfully.");
    }
}