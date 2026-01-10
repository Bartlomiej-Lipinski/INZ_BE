using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Groups.JoinGroupFeatures;
using Mates.Infrastructure.Data.Entities.Groups;

namespace Mates.Tests.Features.Groups;

public class JoinGroupTest : TestBase
{
    [Fact]
    public async Task Handle_ShouldAddUserToGroup_WhenCodeIsValid()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1","Test Group", "#FFFFFF");
        var user = TestDataFactory.CreateUser("user1", "Test","User");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        
        await GenerateCodeToJoinGroup.Handle(
            "g1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GenerateCodeToJoinGroup>.Instance, 
            CancellationToken.None
        );
        
        await JoinGroup.Handle(
            TestDataFactory.CreateJoinGroupRequest(group.Code),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<JoinGroup>.Instance, 
            CancellationToken.None
        );
        
        var groupUser = await dbContext.GroupUsers.FirstOrDefaultAsync(gu => gu.GroupId == "g1" && gu.UserId == "user1");
        groupUser.Should().NotBeNull();
        groupUser.IsAdmin.Should().BeFalse();
        groupUser.AcceptanceStatus.Should().Be(AcceptanceStatus.Pending);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("user1", "Test","User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        var result = await JoinGroup.Handle(
            TestDataFactory.CreateJoinGroupRequest("INVALIDCODE"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<JoinGroup>.Instance, 
            CancellationToken.None
        );
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<Shared.Responses.ApiResponse<string>>>();
        
        var notFoundResult =
            result as Microsoft.AspNetCore.Http.HttpResults.NotFound<Shared.Responses.ApiResponse<string>>;
        notFoundResult!.Value?.Success.Should().BeFalse();
        notFoundResult.Value?.Message.Should().Be("Group not found or code is invalid.");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenCodeIsExpired()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString()); 
        var group = TestDataFactory.CreateGroup("g1", "Test Group", "#FFFFFF");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        await GenerateCodeToJoinGroup.Handle(
            "g1",
            dbContext,
            CreateClaimsPrincipal(),
            CreateHttpContext(),
            NullLogger<GenerateCodeToJoinGroup>.Instance, 
            CancellationToken.None
        );
        
        var existingGroup = await dbContext.Groups.FirstOrDefaultAsync(g => g.Id == "g1");
        existingGroup!.CodeExpirationTime = DateTime.UtcNow.AddMinutes(-1);
        dbContext.Groups.Update(existingGroup);
        await dbContext.SaveChangesAsync();
        var user1 = TestDataFactory.CreateUser("user1", "Test","User");
        dbContext.Users.Add(user1);
        await dbContext.SaveChangesAsync();
        
        var result = await JoinGroup.Handle(
            TestDataFactory.CreateJoinGroupRequest(existingGroup.Code),
            dbContext,
            CreateClaimsPrincipal(user1.Id),
            CreateHttpContext(),
            NullLogger<JoinGroup>.Instance, 
            CancellationToken.None
        );
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<Shared.Responses.ApiResponse<string>>>();
        
        var badRequestResult = 
            result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<Shared.Responses.ApiResponse<string>>;
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("The code has expired.");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenUserAlreadyInGroup()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group", "#FFFFFF");
        var user = TestDataFactory.CreateUser("user1", "Test","User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        await GenerateCodeToJoinGroup.Handle(
            "g1", 
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id), 
            NullLogger<GenerateCodeToJoinGroup>.Instance, 
            CancellationToken.None
        );
        
        var result = await JoinGroup.Handle(
            TestDataFactory.CreateJoinGroupRequest(group.Code), 
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<JoinGroup>.Instance, 
            CancellationToken.None
        );
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<Shared.Responses.ApiResponse<string>>>();
        
        var badRequestResult = 
            result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<Shared.Responses.ApiResponse<string>>;
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("You are already a member of this group.");
    }
}