using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Storage.Categories;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Storage;

public class DeleteFileCategoryTest : TestBase
{
    [Fact]
    public async Task DeleteCategory_Should_Remove_Category_When_No_Files()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        const string groupId = "g1";
        const string userId = "u1";

        var category = TestDataFactory.CreateFileCategory("c1", groupId, "Test Category");
        dbContext.FileCategories.Add(category);
        await dbContext.SaveChangesAsync();
        
        var httpContext = CreateHttpContext(userId);
        httpContext.Items["GroupUser"] = TestDataFactory.CreateGroupUser(userId, groupId, isAdmin: true);
        
        var result = await DeleteFileCategory.Handle(
            groupId,
            category.Id,
            dbContext,
            CreateClaimsPrincipal(userId),
            httpContext,
            NullLogger<DeleteFileCategory>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value!.Success.Should().BeTrue();

        (await dbContext.FileCategories.FindAsync(category.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategory_Should_Return_NotFound_For_Invalid_Category()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        const string groupId = "g1";
        const string userId = "u1";

        var result = await DeleteFileCategory.Handle(
            groupId,
            "nonexistent",
            dbContext,
            CreateClaimsPrincipal(userId),
            CreateHttpContext(userId),
            NullLogger<DeleteFileCategory>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}