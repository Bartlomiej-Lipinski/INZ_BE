using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Settlements;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Settlements;

public class GetUserSettlementsTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await GetUserSettlements.Handle(
            "g1",
            dbContext,
            CreateClaimsPrincipal(),
            CreateHttpContext(),
            NullLogger<GetUserSettlements>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await GetUserSettlements.Handle(
            "nonexistent-group",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetUserSettlements>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Not_Member_Of_Group()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var result = await GetUserSettlements.Handle(
            "g1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetUserSettlements>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_User_Settlements_When_They_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        var otherUser = TestDataFactory.CreateUser("u2", "User2");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser1 = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var groupUser2 = TestDataFactory.CreateGroupUser(otherUser.Id, group.Id);

        var settlement1 = TestDataFactory.CreateSettlement("s1", group.Id, user.Id, otherUser.Id, 100m);
        var settlement2 = TestDataFactory.CreateSettlement("s2", group.Id, user.Id, otherUser.Id, 50m);

        dbContext.Users.AddRange(user, otherUser);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.AddRange(groupUser1, groupUser2);
        dbContext.Settlements.AddRange(settlement1, settlement2);
        await dbContext.SaveChangesAsync();

        var result = await GetUserSettlements.Handle(
            group.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetUserSettlements>.Instance,
            CancellationToken.None
        );

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<SettlementResponseDto>>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().HaveCount(2);
        ok.Value.Data!.Select(s => s.Id).Should().Contain(["s1", "s2"]);
        ok.Value.Data!.All(s => s.GroupId == group.Id).Should().BeTrue();
        ok.Value.Data!.All(s => s.ToUserId == otherUser.Id).Should().BeTrue();
    }
}