using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Timeline;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Timeline;

public class PostTimelineEventTest : TestBase
{
    [Fact]
    public async Task PostTimelineEvent_Should_Create_Event_For_Valid_User_In_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
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
    public async Task PostTimelineEvent_Should_Return_Forbidden_For_User_Not_In_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var otherUser = TestDataFactory.CreateUser("u2", "otherUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(otherUser.Id, group.Id);
        dbContext.Users.AddRange(user, otherUser);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateTimelineEventRequestDto("Some Event", DateTime.UtcNow.AddDays(3));

        var result = await PostTimelineEvent.Handle(
            group.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();    
    }

    [Fact]
    public async Task PostTimelineEvent_Should_Return_BadRequest_For_Invalid_Request()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
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

    [Fact]
    public async Task PostTimelineEvent_Should_Return_NotFound_For_Invalid_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateTimelineEventRequestDto(
            "Event for non-existent group", DateTime.UtcNow.AddDays(1));

        var result = await PostTimelineEvent.Handle(
            "nonExistentGroupId",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}