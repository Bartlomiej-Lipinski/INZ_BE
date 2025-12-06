using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Polls;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Polls;

public class UpdatePollTest : TestBase
{
    [Fact]
    public async Task UpdatePoll_Should_Update_Question_And_Options_Preserving_Votes()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var poll = TestDataFactory.CreatePoll("p1", group.Id, user.Id, "Original question");
        var option1 = TestDataFactory.CreatePollOption("o1", poll.Id, "Option 1");
        var option2 = TestDataFactory.CreatePollOption("o2", poll.Id, "Option 2");

        var voter = TestDataFactory.CreateUser("voter1", "Test","User");
        var groupVoter = TestDataFactory.CreateGroupUser(voter.Id, group.Id);

        option1.VotedUsers.Add(voter);
        dbContext.Users.AddRange(user, voter);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.AddRange(groupUser, groupVoter);
        dbContext.Polls.Add(poll);
        dbContext.PollOptions.AddRange(option1, option2);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreatePollRequestDto("Updated question", [
            new PollOptionDto { Id = option1.Id, Text = "Updated Option 1" },
            new PollOptionDto { Text = "New Option 3" }
        ]);

        var result = await UpdatePoll.Handle(
            group.Id,
            poll.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdatePoll>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data.Should().Be("Poll updated successfully.");
        okResult.Value.Message.Should().Be(poll.Id);
        
        var updatedPoll = await dbContext.Polls
            .Include(p => p.Options).ThenInclude(o => o.VotedUsers)
            .FirstOrDefaultAsync(p => p.Id == poll.Id);

        updatedPoll.Should().NotBeNull();
        updatedPoll.Question.Should().Be("Updated question");
        updatedPoll.Options.Should().HaveCount(2);
        updatedPoll.Options.Any(o => o.Text == "Updated Option 1").Should().BeTrue();
        updatedPoll.Options.Any(o => o.Text == "New Option 3").Should().BeTrue();
        updatedPoll.Options.Any(o => o.Text == "Option 2").Should().BeFalse();

        var updatedOption1 = updatedPoll.Options.First(o => o.Text == "Updated Option 1");
        updatedOption1.VotedUsers.Should().ContainSingle(v => v.Id == voter.Id);
    }
    
    [Fact]
    public async Task UpdatePoll_Should_Return_NotFound_When_Poll_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreatePollRequestDto("New Question", []);

        var result = await UpdatePoll.Handle(
            group.Id,
            "non-existent-poll-id",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdatePoll>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task UpdatePoll_Should_Return_Forbidden_When_User_Is_Not_Creator()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var poll = TestDataFactory.CreatePoll("p1", group.Id, "otherUser", "Original question");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id, isAdmin: false);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.Polls.Add(poll);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreatePollRequestDto("Updated question", []);

        var result = await UpdatePoll.Handle(
            group.Id,
            poll.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdatePoll>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
    
    [Fact]
    public async Task UpdatePoll_Should_Remove_Unsubmitted_Options()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var poll = TestDataFactory.CreatePoll("p1", group.Id, user.Id, "Original question");
        var option1 = TestDataFactory.CreatePollOption("o1", poll.Id, "Option 1");
        var option2 = TestDataFactory.CreatePollOption("o2", poll.Id, "Option 2");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Polls.Add(poll);
        dbContext.PollOptions.AddRange(option1, option2);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreatePollRequestDto("Updated question", [
            new PollOptionDto { Id = option1.Id, Text = "Updated Option 1" }
        ]);

        await UpdatePoll.Handle(
            group.Id,
            poll.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdatePoll>.Instance,
            CancellationToken.None
        );

        var updatedPoll = await dbContext.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == poll.Id);

        updatedPoll.Should().NotBeNull();
        updatedPoll.Options.Should().HaveCount(1);
        updatedPoll.Options.Single().Text.Should().Be("Updated Option 1");
    }
}