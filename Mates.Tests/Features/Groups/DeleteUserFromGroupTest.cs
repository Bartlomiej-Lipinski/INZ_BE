using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Groups;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Groups;

public class DeleteUserFromGroupTest : TestBase
{
    [Fact]
    public async Task DeleteUserFromGroup_Succeeds()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var userId = TestDataFactory.CreateUser("u1", "Test","User");
        var userTwoId = TestDataFactory.CreateUser("u2", "Test","User");
        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF");
        var groupUser = TestDataFactory.CreateGroupUser(userId: userId.Id, groupId: group.Id, isAdmin: true);
        var groupUserTwo = TestDataFactory.CreateGroupUser(userId: userTwoId.Id, groupId: group.Id, isAdmin: false);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        dbContext.GroupUsers.AddRange(groupUser, groupUserTwo);
        await dbContext.SaveChangesAsync();
        var httpContext = CreateHttpContext(userId.Id);
        httpContext.Items["GroupUser"] = groupUser;
        var result = await DeleteUserFromGroup.Handle(
            group.Id,
            userTwoId.Id,
            dbContext,
            CreateClaimsPrincipal(userId.Id),
            NullLogger<DeleteUserFromGroup>.Instance,
            httpContext,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var groupUserInDb = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.UserId == userTwoId.Id && gu.GroupId == group.Id);
        groupUserInDb.Should().BeNull();
        result.As<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>().Value?.Data.Should().Be("User removed from group successfully.");
    }
}