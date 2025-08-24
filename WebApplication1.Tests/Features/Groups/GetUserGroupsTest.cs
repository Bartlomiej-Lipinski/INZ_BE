using FluentAssertions;
using WebApplication1.Features.Groups;

namespace WebApplication1.Tests.Features.Groups;

public class GetUserGroupsTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_EmptyList_When_User_Has_No_Groups()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var response = await GetUserGroups.Handle(
                TestDataFactory.CreateGetUserGroupsRequest("user1"), dbContext, CancellationToken.None);
        
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data.Should().BeEmpty();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Groups_For_User_When_They_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("user1", "Test User");
        var group1 = TestDataFactory.CreateGroup("group1", "Group 1", "#fff", "C1");
        var group2 = TestDataFactory.CreateGroup("group2", "Group 2", "#000", "C2");
            
        dbContext.Users.Add(user);
        dbContext.Groups.AddRange(group1, group2);
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user.Id, group1.Id));
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user.Id, group2.Id));
        await dbContext.SaveChangesAsync();
        
        var response = await GetUserGroups.Handle(
                TestDataFactory.CreateGetUserGroupsRequest(user.Id), dbContext, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Count().Should().Be(2);
        response.Data.Should().ContainEquivalentOf(new GetUserGroups.GroupResponse(group1.Id, group1.Name));
        response.Data.Should().ContainEquivalentOf(new GetUserGroups.GroupResponse(group2.Id, group2.Name));
    }
    
    [Fact]
    public async Task Handle_Should_Return_Only_Groups_For_Specified_User()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user1 = TestDataFactory.CreateUser("user1", "User One");
        var user2 = TestDataFactory.CreateUser("user2", "User Two");
        var group1 = TestDataFactory.CreateGroup("group1", "Group 1", "#fff", "C1");
        var group2 = TestDataFactory.CreateGroup("group2", "Group 2", "#000", "C2");
            
        dbContext.Users.AddRange(user1, user2);
        dbContext.Groups.AddRange(group1, group2);
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user1.Id, group1.Id));
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user2.Id, group2.Id));
        await dbContext.SaveChangesAsync();
        
        var response = await GetUserGroups.Handle(
            new GetUserGroups.GetUserGroupsRequest(user1.Id), dbContext, CancellationToken.None);
        
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Count().Should().Be(1);
        response.Data.First().Id.Should().Be(group1.Id);
        response.Data.First().Name.Should().Be(group1.Name);
    }
}