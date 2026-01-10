using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Polls;
using Mates.Features.Polls.Dtos;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Polls;

public class GetPollByIdTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Poll_When_User_Is_Member()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        var poll = TestDataFactory.CreatePoll("p1", group.Id, user.Id, "Best programming language?");
        var option = TestDataFactory.CreatePollOption("op1", poll.Id, "C#");
        poll.Options.Add(option);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Polls.Add(poll);
        await dbContext.SaveChangesAsync();

        var result = await GetPollById.Handle(
            group.Id,
            poll.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetPollById>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<PollResponseDto>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<PollResponseDto>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data?.Question.Should().Be("Best programming language?");
        ok.Value.Data?.Options.Should().ContainSingle();
        ok.Value.Data?.Options.First().Text.Should().Be("C#");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Poll_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await GetPollById.Handle(
            "g1",
            "nonexistent",
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<GetPollById>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}