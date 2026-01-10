using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Polls;
using Mates.Features.Polls.Dtos;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Polls;

public class PostPollTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Create_Poll_When_User_Is_Group_Member()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var dto = TestDataFactory.CreatePollRequestDto(
            "What should we do this weekend?",
            [
                new PollOptionDto { Text = "Go hiking" },
                new PollOptionDto { Text = "Watch a movie" },
                new PollOptionDto { Text = "Have a barbecue" }
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
    public async Task Handle_Should_Return_BadRequest_When_Question_Is_Missing()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var dto = TestDataFactory.CreatePollRequestDto("", [new PollOptionDto { Text = "Option 1" }]);

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
}