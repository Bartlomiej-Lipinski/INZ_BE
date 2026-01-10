using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Settlements;
using Mates.Features.Settlements.Dtos;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Settlements;

public class GetUserSettlementsTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_User_Settlements_When_They_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var otherUser = TestDataFactory.CreateUser("u2", "Test","User");
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
        ok.Value.Data!.All(s => s.ToUser.Id == otherUser.Id).Should().BeTrue();
    }
}