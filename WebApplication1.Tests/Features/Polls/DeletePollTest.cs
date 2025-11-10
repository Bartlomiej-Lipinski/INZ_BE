using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Polls;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Polls;

public class DeletePollTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Delete_Poll_When_User_Is_Member()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var poll = TestDataFactory.CreatePoll("p1", group.Id, user.Id, question: "Test poll");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Polls.Add(poll);
        await dbContext.SaveChangesAsync();

        var result = await DeletePoll.Handle(
            group.Id,
            poll.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeletePoll>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();

        var deletedPoll = await dbContext.Polls.FirstOrDefaultAsync();
        deletedPoll.Should().BeNull();
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

        var result = await DeletePoll.Handle(
            group.Id,
            "nonexistent",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeletePoll>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Member()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var poll = TestDataFactory.CreatePoll("p1", group.Id, "u1", question: "Test poll");
        dbContext.Groups.Add(group);
        dbContext.Polls.Add(poll);
        await dbContext.SaveChangesAsync();

        var result = await DeletePoll.Handle(
            group.Id,
            poll.Id,
            dbContext,
            CreateClaimsPrincipal("u2"),
            CreateHttpContext("u2"),
            NullLogger<DeletePoll>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
}