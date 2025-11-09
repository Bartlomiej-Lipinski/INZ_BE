using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Timeline;
using WebApplication1.Features.Timeline.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Timeline;

public class GetTimelineTest : TestBase
{
    [Fact]
    public async Task GetTimeline_Should_Return_Timeline_For_Valid_User_In_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser", birthDate: new DateOnly(1990, 1, 1));
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        var groupEvent = TestDataFactory.CreateEvent(
            "e1", 
            group.Id,
            user.Id, 
            "Group Meeting", 
            null,
            DateTime.UtcNow.AddDays(1),
            null, 
            DateTime.Now
        );
        var customEvent = TestDataFactory.CreateTimelineEvent(
            "ce1", group.Id, "Anniversary", DateTime.UtcNow.AddDays(5));

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(groupEvent);
        dbContext.TimelineEvents.Add(customEvent);
        await dbContext.SaveChangesAsync();

        var result = await GetTimeline.Handle(
            group.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetTimeline>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<TimelineEventResponseDto>>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<TimelineEventResponseDto>>>;
        okResult!.Value!.Success.Should().BeTrue();

        var timeline = okResult.Value!.Data!;
        timeline.Should().HaveCount(3);
        timeline.Any(e => e.Title.Contains("Urodziny")).Should().BeTrue();
        timeline.Any(e => e.Title == "Group Meeting").Should().BeTrue();
        timeline.Any(e => e.Title == "Anniversary").Should().BeTrue();
    }
}