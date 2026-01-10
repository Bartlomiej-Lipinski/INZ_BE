using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Storage.Categories;
using WebApplication1.Features.Storage.Dtos;
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

        var postFileCategoryDto = new PostFileCategoryDto
        {
            CategoryName = categoryName
        };

        var result = await PostFileCategory.Handle(
            group.Id,
            postFileCategoryDto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostFileCategory>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Ok<ApiResponse<string>>>();
        var okResult = result as Ok<ApiResponse<string>>;
        okResult!.Value!.Success.Should().BeTrue();

        var categoryInDb = await dbContext.FileCategories.FirstOrDefaultAsync();
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

        var postFileCategoryDto = new PostFileCategoryDto
        {
            CategoryName = categoryName
        };

        var result = await PostFileCategory.Handle(
            group.Id,
            postFileCategoryDto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostFileCategory>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
        var badRequest = result as BadRequest<ApiResponse<string>>;
        badRequest!.Value!.Success.Should().BeFalse();
        badRequest.Value.Message.Should().Contain("Category name is required");
    }
}