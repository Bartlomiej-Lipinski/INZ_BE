using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApplication1.Features.Groups;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class DeleteUserFromGroupTest : TestBase
{
    [Fact]
    public async Task DeleteUserFromGroup_Succeeds()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var logger = new LoggerFactory().CreateLogger<DeleteUserFromGroup>();
        var httpContext = CreateHttpContext();

        var userId = TestDataFactory.CreateUser("u1");
        var userTwoId = TestDataFactory.CreateUser("u2");
        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF");
        var groupUser = TestDataFactory.CreateGroupUser(userId: userId.Id, groupId: group.Id, isAdmin: true);
        var groupUserTwo = TestDataFactory.CreateGroupUser(userId: userTwoId.Id, groupId: group.Id, isAdmin: false);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        dbContext.GroupUsers.AddRange(groupUser, groupUserTwo);
        await dbContext.SaveChangesAsync();
        var result = await DeleteUserFromGroup.Handle(
            group.Id,
            userTwoId.Id,
            dbContext,
            CreateClaimsPrincipal(userId.Id),
            logger,
            httpContext,
            CancellationToken.None
        );
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var groupUserInDb = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.UserId == userTwoId.Id && gu.GroupId == group.Id);
        groupUserInDb.Should().BeNull();
        result.As<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>().Value.Data .Should().Be("User removed from group successfully.");
    }
}