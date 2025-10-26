using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Groups;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class GetIsAdminTest : TestBase
{
    [Fact]
    public async Task GetIsAdmin_ReturnsTrue_ForAdminUser()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var logger = NullLogger<GetIsAdmin>.Instance;
        var user1 = TestDataFactory.CreateUser("user1");
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
            logger,
            httpContext,
            CancellationToken.None);

        var isAdmin = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<bool>>;
        isAdmin.Should().NotBeNull();
        isAdmin.Value?.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetIsAdmin_ReturnsFalse_ForNonAdminUser()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var logger = NullLogger<GetIsAdmin>.Instance;
        var user1 = TestDataFactory.CreateUser("user1");
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
            logger,
            httpContext,
            CancellationToken.None);

        var isAdmin = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<bool>>;
        isAdmin.Should().NotBeNull();
        isAdmin.Value?.Data.Should().BeFalse();
    }
}