using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Groups;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Groups;

public class GetIsAdminTest : TestBase
{
    [Fact]
    public async Task GetIsAdmin_ReturnsTrue_ForAdminUser()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user1 = TestDataFactory.CreateUser("user1", "Test","User");
        var group = TestDataFactory.CreateGroup("group1");
        dbContext.Users.Add(user1);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(
            userId: user1.Id,
            groupId: group.Id,
            isAdmin: true,
            acceptance: Infrastructure.Data.Entities.Groups.AcceptanceStatus.Accepted));
        await dbContext.SaveChangesAsync();

        var result = await GetIsAdmin.Handle(
            groupId: group.Id,
            dbContext,
            CreateClaimsPrincipal(user1.Id),
            NullLogger<GetIsAdmin>.Instance,
            CreateHttpContext(),
            CancellationToken.None);

        var isAdmin = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<bool>>;
        isAdmin.Should().NotBeNull();
        isAdmin.Value?.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetIsAdmin_ReturnsFalse_ForNonAdminUser()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user1 = TestDataFactory.CreateUser("user1", "Test","User");
        var group = TestDataFactory.CreateGroup("group1");
        dbContext.Users.Add(user1);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(
            userId: user1.Id,
            groupId: group.Id,
            isAdmin: false,
            acceptance: Infrastructure.Data.Entities.Groups.AcceptanceStatus.Accepted));
        await dbContext.SaveChangesAsync();

        var result = await GetIsAdmin.Handle(
            groupId: group.Id,
            dbContext,
            CreateClaimsPrincipal(user1.Id),
            NullLogger<GetIsAdmin>.Instance,
            CreateHttpContext(),
            CancellationToken.None);

        var isAdmin = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<bool>>;
        isAdmin.Should().NotBeNull();
        isAdmin.Value?.Data.Should().BeFalse();
    }
}