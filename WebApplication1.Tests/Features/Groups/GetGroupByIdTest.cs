using FluentAssertions;
using WebApplication1.Features.Groups;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class GetGroupByIdTest : TestBase
{
    [Fact]
    public async Task Handle_ShouldReturnOk_WhenGroupExists()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF", code: "ABC123");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await GetGroupById.Handle("test-id", dbContext, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<GetGroupById.GroupResponseDto>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<GetGroupById.GroupResponseDto>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data.Should().NotBeNull();
        okResult.Value?.Data!.Id.Should().Be("test-id");
        okResult.Value?.Data?.Name.Should().Be("Test Group");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();

        // Act
        var result = await GetGroupById.Handle("non-existent", dbContext, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFoundResult = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFoundResult!.Value?.Success.Should().BeFalse();
        notFoundResult.Value?.Message.Should().Be("Group not found");
    }
}