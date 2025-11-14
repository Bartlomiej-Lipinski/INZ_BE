using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Groups;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class GetUserGroupsTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_EmptyList_When_User_Has_No_Groups()
    {
        var user = TestDataFactory.CreateUser("user1", "Test","User");
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var result = await GetUserGroups.Handle(
            CreateClaimsPrincipal(user.Id),
            dbContext, 
            CreateHttpContext(user.Id),
            NullLogger<GetUserGroups>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<GroupResponseDto>>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<GroupResponseDto>>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Should().NotBeNull();
        okResult.Value.Data.Should().BeEmpty();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Groups_For_User_When_They_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("user1", "Test","User");
        var group1 = TestDataFactory.CreateGroup("group1", "Group 1", "#fff", "C1");
        var group2 = TestDataFactory.CreateGroup("group2", "Group 2", "#000", "C2");
        
        dbContext.Users.Add(user);
        dbContext.Groups.AddRange(group1, group2);
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user.Id, group1.Id));
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user.Id, group2.Id));
        await dbContext.SaveChangesAsync();
        
        var result = await GetUserGroups.Handle(
            CreateClaimsPrincipal(user.Id), 
            dbContext,
            CreateHttpContext(user.Id), 
            NullLogger<GetUserGroups>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<GroupResponseDto>>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<GroupResponseDto>>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Should().NotBeNull();
        okResult.Value.Data!.Count.Should().Be(2);
        okResult.Value.Data.Should().ContainEquivalentOf(
            TestDataFactory.CreateGroupResponseDto(group1.Id, group1.Name, "#fff"));
        okResult.Value.Data.Should().ContainEquivalentOf(
            TestDataFactory.CreateGroupResponseDto(group2.Id, group2.Name, "#000"));
    }
    
    [Fact]
    public async Task Handle_Should_Return_Only_Groups_For_Specified_User()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user1 = TestDataFactory.CreateUser("user1", "Test","User");
        var user2 = TestDataFactory.CreateUser("user2", "Test","User");
        var group1 = TestDataFactory.CreateGroup("group1", "Group 1", "#fff", "C1");
        var group2 = TestDataFactory.CreateGroup("group2", "Group 2", "#000", "C2");
        
        dbContext.Users.AddRange(user1, user2);
        dbContext.Groups.AddRange(group1, group2);
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user1.Id, group1.Id));
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user2.Id, group2.Id));
        await dbContext.SaveChangesAsync();
        
        var result = await GetUserGroups.Handle(
            CreateClaimsPrincipal(user1.Id),
            dbContext,
            CreateHttpContext(user1.Id),
            NullLogger<GetUserGroups>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<GroupResponseDto>>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<GroupResponseDto>>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Should().NotBeNull();
        okResult.Value.Data!.Count.Should().Be(1);
        okResult.Value.Data.First().Id.Should().Be(group1.Id);
    }
}