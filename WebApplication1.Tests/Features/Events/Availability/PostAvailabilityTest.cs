﻿using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events.Availability;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events.Availability;

public class PostAvailabilityTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await PostAvailability.Handle(
            "g1",
            "e1",
            TestDataFactory.CreateEventAvailabilityRequestDto(EventAvailabilityStatus.Going),
            dbContext,
            CreateClaimsPrincipal(),
            CreateHttpContext(),
            NullLogger<PostAvailability>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "test");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await PostAvailability.Handle(
            "g1",
            "e1",
            TestDataFactory.CreateEventAvailabilityRequestDto(EventAvailabilityStatus.Going),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<PostAvailability>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbidden_When_User_Not_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "test");
        var group = TestDataFactory.CreateGroup("g1", "test");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        
        var result = await PostAvailability.Handle(
            group.Id,
            "e1",
            TestDataFactory.CreateEventAvailabilityRequestDto(EventAvailabilityStatus.Going),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<PostAvailability>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Event_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "test");
        var group = TestDataFactory.CreateGroup("g1", "test");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await PostAvailability.Handle(
            group.Id,
            "e1",
            TestDataFactory.CreateEventAvailabilityRequestDto(EventAvailabilityStatus.Going),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostAvailability>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Create_New_Availability_When_Not_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "test");
        var group = TestDataFactory.CreateGroup("g1", "test");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var evt = TestDataFactory.CreateEvent(
            "e1", group.Id, user.Id, "test", null, null, DateTime.UtcNow);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateEventAvailabilityRequestDto(EventAvailabilityStatus.Maybe);
        
        var result = await PostAvailability.Handle(
            group.Id,
            evt.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostAvailability>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.EventAvailabilities.CountAsync()).Should().Be(1);
        var availability = await dbContext.EventAvailabilities.FirstAsync();
        availability.UserId.Should().Be(user.Id);
        availability.EventId.Should().Be(evt.Id);
        availability.Status.Should().Be(EventAvailabilityStatus.Maybe);
    }

    [Fact]
    public async Task Handle_Should_Update_Existing_Availability()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "test");
        var group = TestDataFactory.CreateGroup("g1", "test");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var evt = TestDataFactory.CreateEvent(
            "e1", group.Id, user.Id, "test", null, null, DateTime.UtcNow);
        var existing = TestDataFactory.CreateEventAvailability(
            evt.Id, user.Id, EventAvailabilityStatus.Going, DateTime.UtcNow);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        dbContext.EventAvailabilities.Add(existing);
        await dbContext.SaveChangesAsync();
        
        var request = TestDataFactory.CreateEventAvailabilityRequestDto(EventAvailabilityStatus.Maybe);

        var result = await PostAvailability.Handle(
            group.Id,
            evt.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostAvailability>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var updated = await dbContext.EventAvailabilities.FirstAsync();
        updated.Status.Should().Be(EventAvailabilityStatus.Maybe);
    }
}