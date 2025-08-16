using FluentAssertions;
using WebApplication1.Features.Groups;

namespace WebApplication1.Tests.Features.Groups;

public class GetUserGroupsTest : TestBase
{
    [Fact]
    public async Task Handle_ReturnsGroups_WhenUserHasGroups()
    {
        // Arrange
        await using var dbContext = GetInMemoryDbContext();

        const string userId = "user-1";
        var group1 = TestDataFactory.CreateGroup(id: "g1", name: "Admins", color: "Black", code: "A");
        var group2 = TestDataFactory.CreateGroup(id: "g2", name: "Users", color: "Black", code: "A");

        await dbContext.Groups.AddRangeAsync(group1, group2);
        await dbContext.GroupUsers.AddRangeAsync(
            TestDataFactory.CreateGroupUser(userId: userId, groupId: group1.Id),
            TestDataFactory.CreateGroupUser(userId: userId, groupId: group2.Id)
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
        await using var dbContext = GetInMemoryDbContext();

        var request = new GetUserGroups.GetUserGroupsRequest("user-no-groups");

        // Act
        var result = await GetUserGroups.Handle(request, dbContext, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}