using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Shared.Responses;
using WebApplication1.Features.Events.Availability;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Data.Entities.Events;
using Xunit;

namespace WebApplication1.Tests.Features.Events
{
    public class ChoseBestDateForEventTest : TestBase
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

            var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity()); // not authenticated
            var httpContext = new DefaultHttpContext { User = unauthenticated };

            var result = await ChoseBestDateForEvent.Handle(
                "e1",
                "s1",
                dbContext,
                unauthenticated,
                httpContext,
                NullLogger<ChoseBestDateForEvent>.Instance,
                CancellationToken.None
            );

            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Event_Not_Found()
        {
            await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = TestDataFactory.CreateUser("u1", "TestUser");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var result = await ChoseBestDateForEvent.Handle(
                "nonexistent-event",
                "s1",
                dbContext,
                CreateClaimsPrincipal(user.Id),
                CreateHttpContext(user.Id),
                NullLogger<ChoseBestDateForEvent>.Instance,
                CancellationToken.None
            );

            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
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

            var result = await ChoseBestDateForEvent.Handle(
                evt.Id,
                "s1",
                dbContext,
                CreateClaimsPrincipal(user.Id),
                CreateHttpContext(user.Id),
                NullLogger<ChoseBestDateForEvent>.Instance,
                CancellationToken.None
            );

            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
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

            var suggestion = new EventSuggestion
            {
                Id = "s1",
                StartTime = DateTime.UtcNow.AddDays(2)
            };
            evt.Suggestions.Add(suggestion);

            dbContext.Users.Add(user);
            dbContext.Groups.Add(group);
            dbContext.GroupUsers.Add(groupUser);
            dbContext.Events.Add(evt);
            await dbContext.SaveChangesAsync();

            var result = await ChoseBestDateForEvent.Handle(
                evt.Id,
                suggestion.Id,
                dbContext,
                CreateClaimsPrincipal(user.Id),
                CreateHttpContext(user.Id),
                NullLogger<ChoseBestDateForEvent>.Instance,
                CancellationToken.None
            );

            result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

            // reload from db to verify persisted changes
            var dbEvt = await dbContext.Events
                .Include(e => e.Suggestions)
                .FirstAsync(e => e.Id == evt.Id);

            dbEvt.StartDate.Should().Be(suggestion.StartTime);
            dbEvt.Suggestions.Should().BeEmpty();
        }
    }
}