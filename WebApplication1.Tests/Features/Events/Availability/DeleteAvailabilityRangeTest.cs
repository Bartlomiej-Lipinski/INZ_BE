using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events.Availability;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events.Availability;

public class DeleteAvailabilityRangeTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await DeleteAvailabilityRange.Handle(
            "g1",
            "e1",
            dbContext,
            CreateClaimsPrincipal(),
            CreateHttpContext(),
            NullLogger<DeleteAvailabilityRange>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "TestUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await DeleteAvailabilityRange.Handle(
            "g1",
            "e1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<DeleteAvailabilityRange>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbidden_When_User_Not_Member_Of_Group()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "TestUser");
        var group = TestDataFactory.CreateGroup("g1", "TestGroup");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var result = await DeleteAvailabilityRange.Handle(
            group.Id,
            "e1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<DeleteAvailabilityRange>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Event_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "TestUser");
        var group = TestDataFactory.CreateGroup("g1", "TestGroup");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await DeleteAvailabilityRange.Handle(
            group.Id,
            "e1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteAvailabilityRange>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Ok_When_No_Availability_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "TestUser");
        var group = TestDataFactory.CreateGroup("g1", "TestGroup");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var evt = TestDataFactory.CreateEvent("e1", group.Id, user.Id, "Test Event", null, null, DateTime.UtcNow);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var result = await DeleteAvailabilityRange.Handle(
            group.Id,
            evt.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteAvailabilityRange>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.EventAvailabilityRanges.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_Delete_Existing_Availability()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "TestUser");
        var group = TestDataFactory.CreateGroup("g1", "TestGroup");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var evt = TestDataFactory.CreateEvent("e1", group.Id, user.Id, "Test Event", null, null, DateTime.UtcNow);

        var existing = TestDataFactory.CreateEventAvailabilityRange(
            evt.Id, user.Id, DateTime.UtcNow, DateTime.UtcNow.AddHours(2));

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        dbContext.EventAvailabilityRanges.Add(existing);
        await dbContext.SaveChangesAsync();

        var result = await DeleteAvailabilityRange.Handle(
            group.Id,
            evt.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteAvailabilityRange>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.EventAvailabilityRanges.CountAsync()).Should().Be(0);
    }
}