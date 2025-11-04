using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Polls;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Polls;

public class PostPollVoteTest : TestBase
{
    [Fact]
    public async Task PostPollVote_Should_Add_Vote_When_Not_Voted()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var poll = TestDataFactory.CreatePoll("p1", group.Id, user.Id, "Test Question");
        var option = TestDataFactory.CreatePollOption("o1", poll.Id, "Option 1");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Polls.Add(poll);
        dbContext.PollOptions.Add(option);
        await dbContext.SaveChangesAsync();

        var result = await PostPollVote.Handle(
            group.Id,
            poll.Id,
            option.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostPollVote>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value!.Data.Should().Be("Vote added.");

        var updatedOption = await dbContext.PollOptions
            .Include(o => o.VotedUsers)
            .FirstAsync(o => o.Id == option.Id);

        updatedOption.VotedUsers.Should().ContainSingle(u => u.Id == user.Id);
    }

    [Fact]
    public async Task PostPollVote_Should_Remove_Vote_When_Already_Voted()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var poll = TestDataFactory.CreatePoll("p1", group.Id, user.Id, "Test Question");
        var option = TestDataFactory.CreatePollOption("o1", poll.Id, "Option 1");
        option.VotedUsers.Add(user);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Polls.Add(poll);
        dbContext.PollOptions.Add(option);
        await dbContext.SaveChangesAsync();

        var result = await PostPollVote.Handle(
            group.Id,
            poll.Id,
            option.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostPollVote>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value!.Data.Should().Be("Vote removed.");

        var updatedOption = await dbContext.PollOptions
            .Include(o => o.VotedUsers)
            .FirstAsync(o => o.Id == option.Id);

        updatedOption.VotedUsers.Should().BeEmpty();
    }

    [Fact]
    public async Task PostPollVote_Should_Return_Forbidden_If_User_Not_In_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var poll = TestDataFactory.CreatePoll("p1", group.Id, user.Id, "Test Question");
        var option = TestDataFactory.CreatePollOption("o1", poll.Id, "Option 1");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.Polls.Add(poll);
        dbContext.PollOptions.Add(option);
        await dbContext.SaveChangesAsync();

        var result = await PostPollVote.Handle(
            group.Id,
            poll.Id,
            option.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostPollVote>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task PostPollVote_Should_Return_NotFound_If_Group_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await PostPollVote.Handle(
            "nonExistentGroup",
            "pollId",
            "optionId",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostPollVote>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFoundResult = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFoundResult!.Value!.Message.Should().Be("Group not found.");
    }
}