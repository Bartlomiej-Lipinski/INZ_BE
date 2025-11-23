using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Storage;
using WebApplication1.Features.Storage.Categories;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Storage;

public class PostFileCategoryTest : TestBase
{
    [Fact]
    public async Task PostFileCategory_Should_Create_Category_Successfully()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        const string categoryName = "New Category";

        var result = await PostFileCategory.Handle(
            group.Id,
            categoryName,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostFileCategory>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<FileCategory>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<FileCategory>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Name.Should().Be(categoryName);
        okResult.Value.Data.GroupId.Should().Be(group.Id);

        var categoryInDb = await dbContext.FileCategories.FindAsync(okResult.Value.Data.Id);
        categoryInDb.Should().NotBeNull();
        categoryInDb.Name.Should().Be(categoryName);
    }

    [Fact]
    public async Task PostFileCategory_Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        const string categoryName = "";

        var result = await PostFileCategory.Handle(
            group.Id,
            categoryName,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostFileCategory>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value!.Success.Should().BeFalse();
        badRequest.Value.Message.Should().Contain("Category name is required");
    }
}