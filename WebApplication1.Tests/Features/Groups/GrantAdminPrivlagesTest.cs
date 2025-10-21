using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Features.Groups;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class GrantAdminPrivlagesTest : TestBase
{
    [Fact]
    public async Task GrantAdminPrivileges_Should_Grant_Admin_Role_To_User()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<GrantAdminPrivlages>>();
        var userOne = TestDataFactory.CreateUser("user1");
        var userTwo = TestDataFactory.CreateUser("user2");
        var group = TestDataFactory.CreateGroup("group1", "Test Group", "#FFFFFF", "CODE1");
        var groupUserOne = TestDataFactory.CreateGroupUser("user1", "group1", isAdmin: true);
        var groupUserTwo = TestDataFactory.CreateGroupUser("user2", "group1", isAdmin: false);
        dbContext.Users.AddRange(userOne, userTwo);
        dbContext.GroupUsers.AddRange(groupUserOne, groupUserTwo);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var result = await GrantAdminPrivlages
            .Handle(
                new GrantAdminPrivlages.GrantAdminPrivlagesDto("group1", "user2"),
                dbContext,
                CreateClaimsPrincipal(userOne.Id),
                httpContext,
                mockLogger.Object,
                CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var updatedGroupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.UserId == "user2" && gu.GroupId == "group1", CancellationToken.None);
        updatedGroupUser.Should().NotBeNull();
        updatedGroupUser!.IsAdmin.Should().BeTrue();
    }
}