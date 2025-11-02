using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events.Availability;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events.Availability
{
    public class ChooseBestDateForEventTest : TestBase
    {
        [Fact]
        public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
        {
            await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var group = TestDataFactory.CreateGroup("g1", "Test Group");
            TestDataFactory.CreateUser("owner-id", "Owner");
            var evt = TestDataFactory.CreateEvent(
                "e1",
                group.Id,
                "owner-id",
                "Test Event",
                "Details",
                "Online",
                DateTime.UtcNow
            );

            dbContext.Groups.Add(group);
            dbContext.Events.Add(evt);
            await dbContext.SaveChangesAsync();
            
            var result = await ChooseBestDateForEvent.Handle(
                "e1",
                "s1",
                dbContext,
                CreateClaimsPrincipal(),
                CreateHttpContext(),
                NullLogger<ChooseBestDateForEvent>.Instance,
                CancellationToken.None
            );

            result.Should().BeOfType<UnauthorizedHttpResult>();
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Event_Not_Found()
        {
            await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = TestDataFactory.CreateUser("u1", "TestUser");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var result = await ChooseBestDateForEvent.Handle(
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
        public async Task Handle_Should_Return_Forbid_When_User_Not_Member_Of_Group()
        {
            await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = TestDataFactory.CreateUser("u1", "User1");
            var group = TestDataFactory.CreateGroup("g1", "Group1");
            var evt = TestDataFactory.CreateEvent(
                "e1",
                group.Id,
                user.Id,
                "Test Event",
                "Details",
                "Online",
                DateTime.UtcNow
            );

            dbContext.Users.Add(user);
            dbContext.Groups.Add(group);
            dbContext.Events.Add(evt);
            await dbContext.SaveChangesAsync();

            var result = await ChooseBestDateForEvent.Handle(
                evt.Id,
                "s1",
                dbContext,
                CreateClaimsPrincipal(user.Id),
                CreateHttpContext(user.Id),
                NullLogger<ChooseBestDateForEvent>.Instance,
                CancellationToken.None
            );

            result.Should().BeOfType<ForbidHttpResult>();
        }

        [Fact]
        public async Task Handle_Should_Choose_Best_Date_When_Valid()
        {
            await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = TestDataFactory.CreateUser("u1", "User1");
            var group = TestDataFactory.CreateGroup("g1", "Group1");
            var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
            var evt = TestDataFactory.CreateEvent(
                "e1",
                group.Id,
                user.Id,
                "Test Event",
                "Details",
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
}