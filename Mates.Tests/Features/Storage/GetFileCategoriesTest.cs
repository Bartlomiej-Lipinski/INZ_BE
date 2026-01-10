using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Storage;
using Mates.Features.Storage.Categories;
using Mates.Features.Storage.Dtos;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Storage;

public class GetFileCategoriesTest : TestBase
{
    [Fact]
    public async Task GetFileCategories_Should_Return_Categories_For_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var groupId = "g1";
        var userId = "u1";

        var file1 = TestDataFactory.CreateFileCategory("c1", groupId, "Category 1");
        var file2 = TestDataFactory.CreateFileCategory("c2", groupId, "Category 2");
        dbContext.FileCategories.AddRange(file1, file2);
        await dbContext.SaveChangesAsync();

        var result = await GetFileCategories.Handle(
            groupId,
            dbContext,
            CreateHttpContext(userId),
            CreateClaimsPrincipal(userId),
            NullLogger<GetFileCategories>.Instance,
            CancellationToken.None
        );

        var okResult = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<FileCategoryResponseDto>>>>().Subject;
        okResult.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Should().HaveCount(2);
        okResult.Value.Data.Any(c => c.Name == "Category 1").Should().BeTrue();
        okResult.Value.Data.Any(c => c.Name == "Category 2").Should().BeTrue();
    }

    [Fact]
    public async Task GetFileCategories_Should_Return_EmptyList_When_No_Categories()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var groupId = "g1";
        var userId = "u1";

        var result = await GetFileCategories.Handle(
            groupId,
            dbContext,
            CreateHttpContext(userId),
            CreateClaimsPrincipal(userId),
            NullLogger<GetFileCategories>.Instance,
            CancellationToken.None
        );

        var okResult = result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<FileCategoryResponseDto>>>>().Subject;
        okResult.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Should().BeEmpty();
    }
}