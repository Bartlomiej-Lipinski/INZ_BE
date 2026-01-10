using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Events.Availability;
using Mates.Infrastructure.Data.Entities.Events;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Events.Availability;

public class DeleteAvailabilityTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await DeleteAvailability.Handle(
            "nonexistent-group",
            "e1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteAvailability>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Event_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Group 1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await DeleteAvailability.Handle(
            group.Id,
            "nonexistent-event",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteAvailability>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Availability_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Group 1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var evt = TestDataFactory.CreateEvent(
            "e1", group.Id, user.Id, "Test Event", null, DateTime.UtcNow, null, DateTime.UtcNow);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var result = await DeleteAvailability.Handle(
            group.Id,
            evt.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteAvailability>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Delete_Availability_When_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Group 1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var evt = TestDataFactory.CreateEvent(
            "e1", group.Id, user.Id, "Test Event", null, DateTime.UtcNow, null, DateTime.UtcNow);
        var availability = TestDataFactory.CreateEventAvailability(
            evt.Id, user.Id, EventAvailabilityStatus.Going, DateTime.UtcNow);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        dbContext.EventAvailabilities.Add(availability);
        await dbContext.SaveChangesAsync();

        var result = await DeleteAvailability.Handle(
            group.Id,
            evt.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteAvailability>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.EventAvailabilities.CountAsync()).Should().Be(0);
    }
}