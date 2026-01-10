using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Events;
using Mates.Features.Events.Dtos;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Events;

public class GetEventByIdTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await GetEventById.Handle(
            "nonexistent-group",
            "event1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetEventById>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Event_Not_Found()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await GetEventById.Handle(
            group.Id,
            "nonexistent-event",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetEventById>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Event_When_Exists_And_User_Is_Member()
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
            "Some details", 
            DateTime.UtcNow,
            "Online", 
            DateTime.UtcNow
        );

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var result = await GetEventById.Handle(
            group.Id,
            evt.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetEventById>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<EventResponseDto>>>();

        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<EventResponseDto>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Id.Should().Be(evt.Id);
        okResult.Value.Data.Title.Should().Be(evt.Title);
        okResult.Value.Data.GroupId.Should().Be(group.Id);
        okResult.Value.Data.User.Id.Should().Be(user.Id);
    }
}