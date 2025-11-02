using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Polls;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Polls;

public class GetGroupPollsTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_All_Polls_For_Group_When_User_Is_Member()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "testUser");
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

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Has_No_Claims()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await GetGroupPolls.Handle(
            "g1",
            dbContext,
            CreateClaimsPrincipal(),
            CreateHttpContext(),
            NullLogger<GetGroupPolls>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await GetGroupPolls.Handle(
            "non-existent-group",
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<GetGroupPolls>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Member_Of_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupPolls.Handle(
            group.Id,
            dbContext,
            CreateClaimsPrincipal("u2"),
            CreateHttpContext("u2"),
            NullLogger<GetGroupPolls>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
}