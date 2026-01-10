using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Polls;
using Mates.Features.Polls.Dtos;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Polls;

public class GetGroupPollsTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_All_Polls_For_Group_When_User_Is_Member()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);

        var poll1 = TestDataFactory.CreatePoll("p1", group.Id, user.Id, "Question 1");
        var poll2 = TestDataFactory.CreatePoll("p2", group.Id, user.Id, "Question 2");
        dbContext.Polls.AddRange(poll1, poll2);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupPolls.Handle(
            group.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetGroupPolls>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<PollResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<PollResponseDto>>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().HaveCount(2);
        ok.Value.Data.Select(p => p.Question).Should().Contain(["Question 1", "Question 2"]);
    }
}