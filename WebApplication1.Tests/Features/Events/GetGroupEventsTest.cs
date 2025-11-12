using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events;

public class GetGroupEventsTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Events_For_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        group.GroupUsers.Add(groupUser);

        var evt1 = TestDataFactory.CreateEvent(
            "e1", group.Id, user.Id, "Event 1", null, DateTime.UtcNow, null, DateTime.UtcNow);
        var evt2 = TestDataFactory.CreateEvent(
            "e2", group.Id, user.Id, "Event 2", null, DateTime.UtcNow, null, DateTime.UtcNow.AddHours(1));
        dbContext.Events.AddRange(evt1, evt2);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupEvents.Handle(
            group.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetGroupEvents>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<EventResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<EventResponseDto>>>;
        ok!.Value!.Data.Should().HaveCount(2);
        ok.Value.Data.Select(e => e.Id).Should().Contain(["e1", "e2"]);
    }
}