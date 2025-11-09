using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Challenges;
using WebApplication1.Features.Challenges.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Challenges;

public class GetGroupChallengesTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Challenges_For_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        group.GroupUsers.Add(groupUser);

        var challenge1 = TestDataFactory.CreateChallenge(
            "c1",
            group.Id,
            user.Id,
            "Challenge 1",
            "Test challenge 1",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7)
        );

        var challenge2 = TestDataFactory.CreateChallenge(
            "c2",
            group.Id,
            user.Id,
            "Challenge 2",
            "Test challenge 2",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14)
        );

        dbContext.Challenges.AddRange(challenge1, challenge2);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupChallenges.Handle(
            groupId: group.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetGroupChallenges>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<ChallengeResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<ChallengeResponseDto>>>;

        ok!.Value!.Data.Should().HaveCount(2);
        ok.Value.Data.Select(c => c.Id).Should().Contain(["c1", "c2"]);
        ok.Value.Message.Should().Be("Group challenges retrieved successfully.");
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_List_When_No_Challenges()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u2", "emptyUser");
        var group = TestDataFactory.CreateGroup("g2", "Empty Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        group.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupChallenges.Handle(
            groupId: group.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetGroupChallenges>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<ChallengeResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<ChallengeResponseDto>>>;

        ok!.Value!.Data.Should().BeEmpty();
        ok.Value.Message.Should().Be("No challenges found for this group.");
    }
}