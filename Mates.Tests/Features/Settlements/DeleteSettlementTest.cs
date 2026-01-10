using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Settlements;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Settlements;

public class DeleteSettlementTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await DeleteSettlement.Handle(
            "nonexistent-group",
            "s1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteSettlement>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Settlement_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await DeleteSettlement.Handle(
            group.Id,
            "nonexistent-settlement",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteSettlement>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Delete_Settlement_When_Exists_And_User_Is_Owner()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var otherUser = TestDataFactory.CreateUser("u2", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser1 = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var groupUser2 = TestDataFactory.CreateGroupUser(otherUser.Id, group.Id);
        var settlement = TestDataFactory.CreateSettlement("s1", group.Id, user.Id, otherUser.Id, 100m);

        dbContext.Users.AddRange(user, otherUser);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.AddRange(groupUser1, groupUser2);
        dbContext.Settlements.Add(settlement);
        await dbContext.SaveChangesAsync();

        var result = await DeleteSettlement.Handle(
            group.Id,
            settlement.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteSettlement>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().Be("Settlement deleted successfully.");

        var deleted = await dbContext.Settlements.FindAsync(settlement.Id);
        deleted.Should().BeNull();
    }
}