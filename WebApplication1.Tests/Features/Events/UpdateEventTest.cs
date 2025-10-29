using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events;

public class UpdateEventTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Update_Event_When_User_Is_Owner()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, "g1");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var existingEvent = TestDataFactory.CreateEvent(
            "e1", group.Id, user.Id, "Old Title", null, null, DateTime.UtcNow);
        dbContext.Events.Add(existingEvent);
        await dbContext.SaveChangesAsync();

        var updateDto = TestDataFactory.CreateEventRequestDto(
            "New Title", null, null, "Updated Description");
        
        var result = await UpdateEvent.Handle(
            group.Id,
            existingEvent.Id,
            updateDto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<UpdateEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<EventResponseDto>>>();
        var updatedEvent = await dbContext.Events.FirstAsync();
        updatedEvent.Title.Should().Be("New Title");
        updatedEvent.Description.Should().Be("Updated Description");
    }
    
    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Owner()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var owner = TestDataFactory.CreateUser("u1", "testUser");
        var otherUser = TestDataFactory.CreateUser("u2", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUserOwner = TestDataFactory.CreateGroupUser(owner.Id, "g1");
        var groupUserOther = TestDataFactory.CreateGroupUser(otherUser.Id, "g1");
        dbContext.Groups.Add(group);
        dbContext.Users.AddRange(owner, otherUser);
        dbContext.GroupUsers.AddRange(groupUserOwner, groupUserOther);
        
        var existingEvent = TestDataFactory.CreateEvent(
            "e1", group.Id, owner.Id, "Old Title", null, null, DateTime.UtcNow);
        dbContext.Events.Add(existingEvent);
        await dbContext.SaveChangesAsync();

        var updateDto = TestDataFactory.CreateEventRequestDto("New Title");
        
        var result = await UpdateEvent.Handle(
            group.Id,
            existingEvent.Id,
            updateDto,
            dbContext,
            CreateClaimsPrincipal(otherUser.Id),
            CreateHttpContext(),
            NullLogger<UpdateEvent>.Instance,
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
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, "g1");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var updateDto = TestDataFactory.CreateEventRequestDto("New Title");
        
        var result = await UpdateEvent.Handle(
            group.Id,
            "nonexistent-event",
            updateDto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<UpdateEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();    }
    
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);

        var updateDto = TestDataFactory.CreateEventRequestDto("New Title");
        
        var result = await UpdateEvent.Handle(
            "nonexistent-group",
            "event-id",
            updateDto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<UpdateEvent>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();    }
}