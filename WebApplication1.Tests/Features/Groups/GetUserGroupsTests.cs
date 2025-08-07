using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Tests.Features.Groups;

public class GetUserGroupsTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsGroups_WhenUserHasGroups()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var userId = "user-1";
        var group1 = new Group { Id = "g1", Name = "Admins", Code = "A", Color = "Black"};
        var group2 = new Group { Id = "g2", Name = "Users", Code = "A", Color = "Black" };
        dbContext.Groups.AddRange(group1, group2);
        dbContext.GroupUsers.AddRange(
            new GroupUser { UserId = userId, GroupId = group1.Id },
            new GroupUser { UserId = userId, GroupId = group2.Id }
        );
        await dbContext.SaveChangesAsync();

        var request = new GetUserGroups.GetUserGroupsRequest(userId);

        // Act
        var result = await GetUserGroups.Handle(request, dbContext, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().Contain(x => x.Name == "Admins");
        result.Data.Should().Contain(x => x.Name == "Users");
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenUserHasNoGroups()
    {
        // Arrange
        await using var dbContext = CreateDbContext();

        var request = new GetUserGroups.GetUserGroupsRequest("user-no-groups");

        // Act
        var result = await GetUserGroups.Handle(request, dbContext, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
