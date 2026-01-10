using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Timeline;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Timeline;

public class PostTimelineEventTest : TestBase
{
    [Fact]
    public async Task PostTimelineEvent_Should_Create_Event_For_Valid_User_In_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateTimelineEventRequestDto(
            "Important Deadline", DateTime.UtcNow.AddDays(2), "Submit the project proposal");
        
        var result = await PostTimelineEvent.Handle(
            group.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value!.Success.Should().BeTrue();

        var timelineEvent = dbContext.TimelineEvents.FirstOrDefault();
        timelineEvent.Should().NotBeNull();
        timelineEvent.Title.Should().Be("Important Deadline");
    }

    [Fact]
    public async Task PostTimelineEvent_Should_Return_BadRequest_For_Invalid_Request()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateTimelineEventRequestDto("", default);

        var result = await PostTimelineEvent.Handle(
            group.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value!.Success.Should().BeFalse();
        badRequest.Value!.Message.Should().Contain("Title and date are required");
    }
}