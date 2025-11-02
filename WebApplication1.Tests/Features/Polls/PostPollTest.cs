using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Polls;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Polls;

public class PostPollTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Create_Poll_When_User_Is_Group_Member()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var dto = TestDataFactory.CreatePollRequestDto(
            "What should we do this weekend?",
            [
                new PollOptionRequestDto { Text = "Go hiking" },
                new PollOptionRequestDto { Text = "Watch a movie" },
                new PollOptionRequestDto { Text = "Have a barbecue" }
            ]
        );

        var result = await PostPoll.Handle(
            group.Id,
            dto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostPoll>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().NotBeNullOrEmpty();

        var created = await dbContext.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync();
        created.Should().NotBeNull();
        created.Question.Should().Be("What should we do this weekend?");
        created.GroupId.Should().Be(group.Id);
        created.CreatedByUserId.Should().Be(user.Id);
        created.Options.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Has_No_Claims()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var dto = TestDataFactory.CreatePollRequestDto(
            "Test question", [new PollOptionRequestDto { Text = "Option 1" }]);

        var result = await PostPoll.Handle(
            "g1",
            dto,
            dbContext,
            CreateClaimsPrincipal(),
            CreateHttpContext(),
            NullLogger<PostPoll>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Question_Is_Missing()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var dto = TestDataFactory.CreatePollRequestDto("", [new PollOptionRequestDto { Text = "Option 1" }]);

        var result = await PostPoll.Handle(
            "g1",
            dto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostPoll>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Member_Of_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var dto = TestDataFactory.CreatePollRequestDto("Test question", [new PollOptionRequestDto { Text = "Option 1" }]);

        var result = await PostPoll.Handle(
            "g1",
            dto,
            dbContext,
            CreateClaimsPrincipal("u2"),
            CreateHttpContext("u2"),
            NullLogger<PostPoll>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var dto = TestDataFactory.CreatePollRequestDto("Test question", [new PollOptionRequestDto { Text = "Option 1" }]);

        var result = await PostPoll.Handle(
            "nonexistent-group",
            dto,
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<PostPoll>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}