using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Timeline;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Timeline;

public class UpdateTimelineEventTest : TestBase
{
    [Fact]
    public async Task UpdateTimelineEvent_Should_Update_Event_For_Valid_User_In_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var timelineEvent = TestDataFactory.CreateTimelineEvent("e1", group.Id, "Old Title", DateTime.UtcNow.AddDays(1));
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.TimelineEvents.Add(timelineEvent);
        await dbContext.SaveChangesAsync();

        var updatedRequest = TestDataFactory.CreateTimelineEventRequestDto(
            "Updated Title", DateTime.UtcNow.AddDays(3), "Updated Description");

        var result = await UpdateTimelineEvent.Handle(
            group.Id,
            timelineEvent.Id,
            updatedRequest,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
    
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value!.Success.Should().BeTrue();

        var updatedEvent = dbContext.TimelineEvents.FirstOrDefault(te => te.Id == timelineEvent.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.Title.Should().Be("Updated Title");
        updatedEvent.Description.Should().Be("Updated Description");
    }
    
    [Fact]
    public async Task UpdateTimelineEvent_Should_Return_NotFound_When_Event_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var updatedRequest = TestDataFactory.CreateTimelineEventRequestDto(
            "Updated Title", DateTime.UtcNow.AddDays(3), "Updated Description");

        var result = await UpdateTimelineEvent.Handle(
            group.Id,
            "nonexistentEventId",
            updatedRequest,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task UpdateTimelineEvent_Should_Return_BadRequest_When_Data_Invalid()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var timelineEvent = TestDataFactory.CreateTimelineEvent("e1", group.Id, "Old Title", DateTime.UtcNow.AddDays(1));
        
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.TimelineEvents.Add(timelineEvent);
        await dbContext.SaveChangesAsync();

        var invalidRequest = TestDataFactory.CreateTimelineEventRequestDto(
            "", DateTime.MinValue, "Description");

        var result = await UpdateTimelineEvent.Handle(
            group.Id,
            timelineEvent.Id,
            invalidRequest,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateTimelineEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }
}