using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events.Availability;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events.Availability;

public class PostAvailabilityRangeTest :TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "TestUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateAvailabilityRangeRequestDto(
            DateTime.UtcNow.Date.AddHours(10),
            numberOfRanges: 1,
            rangeLengthHours: 2,
            gapBetweenRangesHours: 1);

        var result = await PostAvailabilityRange.Handle(
            "g1",
            "e1",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostAvailabilityRange>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
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

        var request = TestDataFactory.CreateAvailabilityRangeRequestDto(
            DateTime.UtcNow.Date.AddHours(10),
            numberOfRanges: 1,
            rangeLengthHours: 2,
            gapBetweenRangesHours: 1);

        var result = await PostAvailabilityRange.Handle(
            group.Id,
            "e1",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostAvailabilityRange>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Create_New_Ranges_When_Valid()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "TestUser");
        var group = TestDataFactory.CreateGroup("g1", "TestGroup");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var evt = TestDataFactory.CreateEvent(
            "e1", group.Id, user.Id, "Test Event", null, DateTime.UtcNow, null, DateTime.UtcNow);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Events.Add(evt);
        await dbContext.SaveChangesAsync();
        
        var request = TestDataFactory.CreateAvailabilityRangeRequestDto(
            DateTime.UtcNow.Date.AddHours(10),
            numberOfRanges: 1,
            rangeLengthHours: 2,
            gapBetweenRangesHours: 1);

        var result = await PostAvailabilityRange.Handle(
            group.Id,
            evt.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostAvailabilityRange>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<AvailabilityRangeResponseDto>>>>();
        (await dbContext.EventAvailabilityRanges.CountAsync()).Should().Be(1);
    }
}