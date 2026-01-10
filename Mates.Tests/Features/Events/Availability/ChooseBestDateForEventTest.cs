using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Events.Availability;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Events.Availability;

public class ChooseBestDateForEventTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Event_Not_Found()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await ChooseBestDateForEvent.Handle(
            group.Id,
            "nonexistent-event",
            "s1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<ChooseBestDateForEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Choose_Best_Date_When_Valid()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var evt = TestDataFactory.CreateEvent(
            "e1",
            group.Id,
            user.Id,
            "Test Event",
            "Details",
            DateTime.UtcNow,
            "Online",
            DateTime.UtcNow
        );

        var suggestion = TestDataFactory.CreateEventSuggestion("s1", evt.Id, DateTime.UtcNow.AddDays(2), 3);
        evt.Suggestions.Add(suggestion);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var result = await ChooseBestDateForEvent.Handle(
            group.Id,
            evt.Id,
            suggestion.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<ChooseBestDateForEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Ok<ApiResponse<string>>>();

        var dbEvt = await dbContext.Events
            .Include(e => e.Suggestions)
            .FirstAsync(e => e.Id == evt.Id);

        dbEvt.StartDate.Should().Be(suggestion.StartTime);
        dbEvt.Suggestions.Should().BeEmpty();
    }
}