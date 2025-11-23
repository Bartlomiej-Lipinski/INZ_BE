using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Storage;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Storage;

public class DeleteFileCategoryTest : TestBase
{
    [Fact]
    public async Task DeleteCategory_Should_Remove_Category_When_No_Files()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var groupId = "g1";
        var userId = "u1";

        var category = TestDataFactory.CreateFileCategory("c1", groupId, "Test Category");
        dbContext.FileCategories.Add(category);
        await dbContext.SaveChangesAsync();

        var result = await DeleteFileCategory.Handle(
            groupId,
            category.Id,
            dbContext,
            CreateClaimsPrincipal(userId),
            CreateHttpContext(userId),
            NullLogger<DeleteFileCategory>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value!.Success.Should().BeTrue();

        (await dbContext.FileCategories.FindAsync(category.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategory_Should_Set_CategoryId_Null_For_Files()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var groupId = "g1";
        var userId = "u1";

        var category = TestDataFactory.CreateFileCategory("c2", groupId, "Category With Files");
        var file1 = TestDataFactory.CreateStoredFile(
            "f1", 
            groupId, 
            "file1.pdf",
            "application/pdf",
            100,
            "url1",
            DateTime.UtcNow,
            null,
            EntityType.Material,
            userId, 
            category.Id 
        );
        var file2 = TestDataFactory.CreateStoredFile(
            "f2", 
            groupId, 
            "file2.pdf",
            "application/pdf",
            200,
            "url2",
            DateTime.UtcNow,
            null,
            EntityType.Material,
            userId, 
            category.Id 
        );

        dbContext.FileCategories.Add(category);
        dbContext.StoredFiles.AddRange(file1, file2);
        await dbContext.SaveChangesAsync();

        await DeleteFileCategory.Handle(
            groupId,
            category.Id,
            dbContext,
            CreateClaimsPrincipal(userId),
            CreateHttpContext(userId),
            NullLogger<DeleteFileCategory>.Instance,
            CancellationToken.None
        );

        var updatedFiles = dbContext.StoredFiles.Where(f => f.Id == "f1" || f.Id == "f2").ToList();
        updatedFiles.All(f => f.CategoryId == null).Should().BeTrue();

        (await dbContext.FileCategories.FindAsync(category.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategory_Should_Return_NotFound_For_Invalid_Category()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var groupId = "g1";
        var userId = "u1";

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