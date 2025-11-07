using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Timeline;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Timeline;

public class DeleteTimelineEventTest : TestBase
{
    [Fact]
    public async Task DeleteTimelineEvent_Should_Delete_Event_For_Valid_User_In_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User 1");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var timelineEvent = TestDataFactory.CreateTimelineEvent("e1", group.Id, "Event title", DateTime.UtcNow);
        
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.TimelineEvents.Add(timelineEvent);
        await dbContext.SaveChangesAsync();

        var result = await DeleteTimelineEvent.Handle(
            group.Id,
            timelineEvent.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        dbContext.TimelineEvents.Any(te => te.Id == timelineEvent.Id).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTimelineEvent_Should_Return_Unauthorized_When_No_User()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await DeleteTimelineEvent.Handle(
            "g1",
            "e1",
            dbContext,
            CreateClaimsPrincipal(null),
            CreateHttpContext(null),
            NullLogger<DeleteTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task DeleteTimelineEvent_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User 1");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await DeleteTimelineEvent.Handle(
            "missing-group",
            "e1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task DeleteTimelineEvent_Should_Return_Forbidden_When_User_Not_In_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User 1");
        var group = TestDataFactory.CreateGroup("g1", "Group 1");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var result = await DeleteTimelineEvent.Handle(
            group.Id,
            "e1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task DeleteTimelineEvent_Should_Return_NotFound_When_Event_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User 1");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await DeleteTimelineEvent.Handle(
            group.Id,
            "missing-event",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}