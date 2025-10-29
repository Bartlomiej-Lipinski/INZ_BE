﻿using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events;

public class CalculateBestDateForEventTests : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Event_Not_Found()
    {
        // Arrange
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext();
        var claims = CreateClaimsPrincipal(user.Id);
        var logger = NullLogger<CalculateBestDateForEvent>.Instance;

        // Act
        var result = await CalculateBestDateForEvent.Handle(
            "non-existent-id",
            dbContext,
            claims,
            httpContext,
            logger,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbidden_When_User_Not_In_Group()
    {
        // Arrange
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var evt = TestDataFactory.CreateEvent("e1", "g1", "u1", "Test Event", "Description", "Location",
            DateTime.UtcNow.AddDays(1));

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext();
        var claims = CreateClaimsPrincipal(user.Id);
        var logger = NullLogger<CalculateBestDateForEvent>.Instance;

        // Act
        var result = await CalculateBestDateForEvent.Handle(
            evt.Id,
            dbContext,
            claims,
            httpContext,
            logger,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_Best_Date_When_Valid()
    {
        // Arrange
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 7);

        var evt = TestDataFactory.CreateEvent("e1", group.Id, user.Id, "Test Event", "Description", "Location",
            startDate);
        evt.EndDate = endDate;
        evt.Availabilities = new List<EventAvailability>
        {
            TestDataFactory.CreateEventAvailability(evt.Id, user.Id, EventAvailabilityStatus.Going, DateTime.Now),
        };
        evt.AvailabilityRanges = new List<EventAvailabilityRange>
        {
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user.Id, startDate.AddHours(10),
                startDate.AddHours(16))
        };

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext();
        var claims = CreateClaimsPrincipal(user.Id);
        var logger = NullLogger<CalculateBestDateForEvent>.Instance;

        // Act
        var result = await CalculateBestDateForEvent.Handle(
            evt.Id,
            dbContext,
            claims,
            httpContext,
            logger,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<Ok<ApiResponse<List<(DateTime date, int availablePeople)>>>>();
        var ok = result as Ok<ApiResponse<List<(DateTime date, int availablePeople)>>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().NotBeNull();
        ok.Value.Data.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void GetBestDateAndTime_Should_Select_Date_With_Most_Points()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var user1 = TestDataFactory.CreateUser("user-1", "User1");
        var user2 = TestDataFactory.CreateUser("user-2", "User2");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");

        var evt = TestDataFactory.CreateEvent("e1", group.Id, user1.Id, "Test Event", "Description", "Location",
            startDate);
        evt.EndDate = startDate.AddDays(7);
        evt.Availabilities = new List<EventAvailability>
        {
            new() { UserId = user1.Id, Status = EventAvailabilityStatus.Going, EventId = evt.Id },
            new() { UserId = user2.Id, Status = EventAvailabilityStatus.Going, EventId = evt.Id }
        };
        evt.AvailabilityRanges = new List<EventAvailabilityRange>
        {
            new()
            {
                UserId = user1.Id, EventId = evt.Id, AvailableFrom = startDate.AddHours(10),
                AvailableTo = startDate.AddHours(16)
            },
            new()
            {
                UserId = user2.Id, EventId = evt.Id, AvailableFrom = startDate.AddHours(12),
                AvailableTo = startDate.AddHours(18)
            }
        };

        // Act
        var results = CalculateBestDateForEvent.GetBestDateAndTime(evt);

        // Assert
        results.Should().HaveCountGreaterThan(0);
        var bestResult = results.First();
        bestResult.date.Date.Should().Be(startDate.Date);
        bestResult.date.Hour.Should().BeInRange(12, 16);
    }

    [Fact]
    public void GetBestDateAndTime_Should_Return_Fallback_When_No_Availabilities()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var user = TestDataFactory.CreateUser("u1", "User1");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");

        var evt = TestDataFactory.CreateEvent("e1", group.Id, user.Id, "Test Event", "Description", "Location",
            startDate);
        evt.StartDate = startDate;
        evt.EndDate = startDate.AddDays(7);
        evt.Availabilities = new List<EventAvailability>();
        evt.AvailabilityRanges = new List<EventAvailabilityRange>();

        // Act
        var results = CalculateBestDateForEvent.GetBestDateAndTime(evt);

        // Assert
        results.Should().HaveCount(1);
        var bestResult = results.First();
        bestResult.date.Date.Should().Be(startDate.Date);
        bestResult.date.Hour.Should().Be(9);
    }


    [Fact]
    public void GetBestDateAndTime_Should_Handle_Overlapping_Availabilities()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var user1 = TestDataFactory.CreateUser("user-1", "User1");
        var user2 = TestDataFactory.CreateUser("user-2", "User2");
        var user3 = TestDataFactory.CreateUser("user-3", "User3");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");

        var evt = TestDataFactory.CreateEvent("e1", group.Id, user1.Id, "Test Event", "Description", "Location",
            startDate);
        evt.EndDate = startDate.AddDays(2);
        evt.Availabilities = new List<EventAvailability>
        {
            new() { UserId = user1.Id, Status = EventAvailabilityStatus.Going, EventId = evt.Id },
            new() { UserId = user2.Id, Status = EventAvailabilityStatus.Going, EventId = evt.Id },
            new() { UserId = user3.Id, Status = EventAvailabilityStatus.Maybe, EventId = evt.Id }
        };
        evt.AvailabilityRanges = new List<EventAvailabilityRange>
        {
            new()
            {
                UserId = user1.Id, EventId = evt.Id, AvailableFrom = startDate.AddHours(10),
                AvailableTo = startDate.AddHours(16)
            },
            new()
            {
                UserId = user2.Id, EventId = evt.Id, AvailableFrom = startDate.AddHours(12),
                AvailableTo = startDate.AddHours(18)
            },
            new()
            {
                UserId = user3.Id, EventId = evt.Id, AvailableFrom = startDate.AddHours(14),
                AvailableTo = startDate.AddHours(17)
            }
        };

        // Act
        var results = CalculateBestDateForEvent.GetBestDateAndTime(evt);

        // Assert
        results.Should().HaveCountGreaterThan(0);
        var bestResult = results.First();
        bestResult.date.Date.Should().Be(startDate.Date);
        bestResult.date.Hour.Should().BeInRange(14, 16);
    }

    [Fact]
    public void GetBestDateAndTime_Should_Select_Best_Date_With_Multiple_AvailabilityRanges()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var user1 = TestDataFactory.CreateUser("user-1", "User1");
        var user2 = TestDataFactory.CreateUser("user-2", "User2");
        var user3 = TestDataFactory.CreateUser("user-3", "User3");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");

        var evt = TestDataFactory.CreateEvent("e1", group.Id, user1.Id, "Test Event", "Description", "Location",
            startDate);
        evt.EndDate = startDate.AddDays(5);
        evt.Availabilities = new List<EventAvailability>
        {
            TestDataFactory.CreateEventAvailability(evt.Id, user1.Id, EventAvailabilityStatus.Going, DateTime.Now),
            TestDataFactory.CreateEventAvailability(evt.Id, user2.Id, EventAvailabilityStatus.Going, DateTime.Now),
            TestDataFactory.CreateEventAvailability(evt.Id, user3.Id, EventAvailabilityStatus.Going, DateTime.Now)
        };
        evt.AvailabilityRanges = new List<EventAvailabilityRange>
        {
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user1.Id, new DateTime(2024, 1, 1, 10, 0, 0),
                new DateTime(2024, 1, 1, 12, 0, 0)),
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user1.Id, new DateTime(2024, 1, 2, 12, 0, 0),
                new DateTime(2024, 1, 2, 18, 0, 0)),
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user2.Id, new DateTime(2024, 1, 2, 14, 0, 0),
                new DateTime(2024, 1, 2, 16, 0, 0)),
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user1.Id, new DateTime(2024, 1, 3, 10, 0, 0),
                new DateTime(2024, 1, 3, 18, 0, 0)),
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user2.Id, new DateTime(2024, 1, 3, 13, 0, 0),
                new DateTime(2024, 1, 3, 17, 0, 0)),
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user3.Id, new DateTime(2024, 1, 3, 12, 0, 0),
                new DateTime(2024, 1, 3, 15, 0, 0)),
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user2.Id, new DateTime(2024, 1, 4, 9, 0, 0),
                new DateTime(2024, 1, 4, 12, 0, 0)),
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user3.Id, new DateTime(2024, 1, 4, 10, 0, 0),
                new DateTime(2024, 1, 4, 14, 0, 0)),
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user3.Id, new DateTime(2024, 1, 5, 15, 0, 0),
                new DateTime(2024, 1, 5, 18, 0, 0)),
            TestDataFactory.CreateEventAvailabilityRange(evt.Id, user1.Id, new DateTime(2024, 1, 5, 8, 0, 0),
                new DateTime(2024, 1, 5, 10, 0, 0))
        };

        // Act
        var results = CalculateBestDateForEvent.GetBestDateAndTime(evt);

        // Assert
        results.Should().HaveCount(3, "metoda zwraca maksymalnie 3 najlepsze daty");

        var bestResult = results.First();
        bestResult.date.Date.Should().Be(new DateTime(2024, 1, 3),
            "dzień 3 stycznia ma największe nakładanie się dostępności wszystkich trzech użytkowników");
        bestResult.date.Hour.Should()
            .Be(13, "godzina 13 to początek przedziału gdzie wszyscy trzej użytkownicy są dostępni");
    }
}