using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events;

public class DeleteEventTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await DeleteEvent.Handle(
            "group1",
            "event1",
            dbContext,
            CreateClaimsPrincipal(),
            CreateHttpContext(),
            NullLogger<DeleteEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await DeleteEvent.Handle(
            "nonexistent-group",
            "event1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Not_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var result = await DeleteEvent.Handle(
            group.Id,
            "event1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Event_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await DeleteEvent.Handle(
            group.Id,
            "nonexistent-event",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Owner_Or_Admin()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var owner = TestDataFactory.CreateUser("u1", "owner");
        var otherUser = TestDataFactory.CreateUser("u2", "other");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Users.AddRange(owner, otherUser);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.AddRange(
            TestDataFactory.CreateGroupUser(owner.Id, group.Id),
            TestDataFactory.CreateGroupUser(otherUser.Id, group.Id)
        );

        var evt = TestDataFactory.CreateEvent(
            "e1", group.Id, owner.Id, "Event title", null, null, DateTime.UtcNow);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var result = await DeleteEvent.Handle(
            group.Id,
            evt.Id,
            dbContext,
            CreateClaimsPrincipal(otherUser.Id),
            CreateHttpContext(otherUser.Id),
            NullLogger<DeleteEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Delete_Event_When_User_Is_Owner()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var owner = TestDataFactory.CreateUser("u1", "owner");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Users.Add(owner);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(owner.Id, group.Id));

        var evt = TestDataFactory.CreateEvent(
            "e1", group.Id, owner.Id, "Event title", null, null, DateTime.UtcNow);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var result = await DeleteEvent.Handle(
            group.Id,
            evt.Id,
            dbContext,
            CreateClaimsPrincipal(owner.Id),
            CreateHttpContext(owner.Id),
            NullLogger<DeleteEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        var eventExists = await dbContext.Events.AnyAsync(e => e.Id == evt.Id);
        eventExists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Delete_Event_When_User_Is_Group_Admin()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var owner = TestDataFactory.CreateUser("u1", "owner");
        var admin = TestDataFactory.CreateUser("u2", "admin");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Users.AddRange(owner, admin);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.AddRange(
            TestDataFactory.CreateGroupUser(owner.Id, group.Id),
            TestDataFactory.CreateGroupUser(admin.Id, group.Id, isAdmin: true)
        );

        var evt = TestDataFactory.CreateEvent(
            "e1", group.Id, owner.Id, "Event title", null, null, DateTime.UtcNow);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var result = await DeleteEvent.Handle(
            group.Id,
            evt.Id,
            dbContext,
            CreateClaimsPrincipal(admin.Id),
            CreateHttpContext(admin.Id),
            NullLogger<DeleteEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        var eventExists = await dbContext.Events.AnyAsync(e => e.Id == evt.Id);
        eventExists.Should().BeFalse();
    }
}