using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Infrastructure.Data.Entities;
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
        var evt = TestDataFactory.CreateEvent("e1", "g1", "u1", "Test Event", "Description", "Location", DateTime.UtcNow.AddDays(1));

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

        var evt = TestDataFactory.CreateEvent("e1", group.Id, user.Id, "Test Event", "Description", "Location", startDate);
        evt.EndDate = endDate;
        evt.Availabilities = new List<EventAvailability>
        {
            new()
            {
                UserId = user.Id,
                Status = EventAvailabilityStatus.Going,
                EventId = evt.Id
            }
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
        result.Should().BeOfType<Ok<ApiResponse<List<CalculateBestDateForEvent.BestDateResult>>>>();
        var ok = result as Ok<ApiResponse<List<CalculateBestDateForEvent.BestDateResult>>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().NotBeNull();
        ok.Value.Data.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void GetBestDateAndTime_Should_Select_Date_With_Most_Points()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var evt = new Event
        {
            StartDate = startDate,
            EndDate = startDate.AddDays(7),
            Availabilities = new List<EventAvailability>
            {
                new()
                {
                    UserId = "user-1",
                    Status = EventAvailabilityStatus.Going
                },
                new()
                {
                    UserId = "user-2",
                    Status = EventAvailabilityStatus.Going
                }
            }
        };

        // Act
        var results = CalculateBestDateForEvent.GetBestDateAndTime(evt);

        // Assert
        results.Should().HaveCountGreaterThan(0);
        var bestResult = results.First();
        bestResult.date.Should().Be(startDate);
        bestResult.time.Hour.Should().Be(9);
    }

    [Fact]
    public void GetBestDateAndTime_Should_Return_Fallback_When_No_Availabilities()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var evt = new Event
        {
            StartDate = startDate,
            EndDate = startDate.AddDays(7),
            Availabilities = new List<EventAvailability>()
        };

        // Act
        var results = CalculateBestDateForEvent.GetBestDateAndTime(evt);

        // Assert
        results.Should().HaveCountGreaterThan(0);
        var bestResult = results.First();
        bestResult.date.Should().Be(startDate);
        bestResult.time.Hour.Should().Be(9);
    }

    [Fact]
    public void GetBestDateAndTime_Should_Handle_Overlapping_Availabilities()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var evt = new Event
        {
            StartDate = startDate,
            EndDate = startDate.AddDays(2),
            Availabilities = new List<EventAvailability>
            {
                new()
                {
                    UserId = "user-1",
                    Status = EventAvailabilityStatus.Going
                },
                new()
                {
                    UserId = "user-1",
                    Status = EventAvailabilityStatus.Going
                },
                new()
                {
                    UserId = "user-2",
                    Status = EventAvailabilityStatus.Going
                },
                new()
                {
                    UserId = "user-3",
                    Status = EventAvailabilityStatus.Maybe
                }
            }
        };

        // Act
        var results = CalculateBestDateForEvent.GetBestDateAndTime(evt);

        // Assert
        results.Should().HaveCountGreaterThan(0);
        var bestResult = results.First();
        bestResult.date.Should().Be(startDate);
        bestResult.time.Hour.Should().Be(9);
    }

    [Fact]
    public void GetBestDateAndTime_Should_Select_Best_Date_With_Multiple_AvailabilityRanges()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var evt = new Event
        {
            Id = "event-1",
            StartDate = startDate,
            EndDate = startDate.AddDays(5),
            Availabilities = new List<EventAvailability>
            {
                new() { UserId = "user-1", Status = EventAvailabilityStatus.Going, EventId = "event-1" },
                new() { UserId = "user-2", Status = EventAvailabilityStatus.Going, EventId = "event-1" },
                new() { UserId = "user-3", Status = EventAvailabilityStatus.Going, EventId = "event-1" }
            },
            AvailabilityRanges = new List<EventAvailabilityRange>
            {
                // Day 1: user-1 (2 godziny)
                new() { UserId = "user-1", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 1, 10, 0, 0), AvailableTo = new DateTime(2024, 1, 1, 12, 0, 0) },

                // Day 2: user-1 i user-2 (nakładające się 14-16)
                new() { UserId = "user-1", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 2, 12, 0, 0), AvailableTo = new DateTime(2024, 1, 2, 18, 0, 0) },
                new() { UserId = "user-2", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 2, 14, 0, 0), AvailableTo = new DateTime(2024, 1, 2, 16, 0, 0) },

                // Day 3: wszyscy trzej użytkownicy (13-15) - NAJLEPSZA DATA
                new() { UserId = "user-1", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 3, 10, 0, 0), AvailableTo = new DateTime(2024, 1, 3, 18, 0, 0) },
                new() { UserId = "user-2", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 3, 13, 0, 0), AvailableTo = new DateTime(2024, 1, 3, 17, 0, 0) },
                new() { UserId = "user-3", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 3, 12, 0, 0), AvailableTo = new DateTime(2024, 1, 3, 15, 0, 0) },

                // Day 4: user-2 i user-3
                new() { UserId = "user-2", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 4, 9, 0, 0), AvailableTo = new DateTime(2024, 1, 4, 12, 0, 0) },
                new() { UserId = "user-3", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 4, 10, 0, 0), AvailableTo = new DateTime(2024, 1, 4, 14, 0, 0) },

                // Day 5: tylko user-3
                new() { UserId = "user-3", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 5, 15, 0, 0), AvailableTo = new DateTime(2024, 1, 5, 18, 0, 0) },
                new() { UserId = "user-1", EventId = "event-1", AvailableFrom = new DateTime(2024, 1, 5, 8, 0, 0), AvailableTo = new DateTime(2024, 1, 5, 10, 0, 0) }
            }
        };

        // Act
        var results = CalculateBestDateForEvent.GetBestDateAndTime(evt);

        // Assert
        results.Should().HaveCountGreaterThan(0);
        results.Should().HaveCountGreaterThanOrEqualTo(3, "metoda zwraca maksymalnie 3 najlepsze daty");

        var bestResult = results.First();
        bestResult.date.Should().Be(new DateTime(2024, 1, 3), "dzień 3 stycznia ma największe nakładanie się dostępności wszystkich trzech użytkowników");
        bestResult.time.Hour.Should().BeInRange(13, 15, "godziny 13-15 to przedział gdzie wszyscy trzej użytkownicy są dostępni");
        bestResult.score.Should().BeGreaterThan(0);

        // Sprawdź czy wyniki są posortowane malejąco według punktów
        for (int i = 0; i < results.Count - 1; i++)
        {
            results[i].score.Should().BeGreaterThanOrEqualTo(results[i + 1].score, "wyniki powinny być posortowane malejąco według punktów");
        }
    }
}