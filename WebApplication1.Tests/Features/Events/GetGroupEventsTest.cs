using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events;

public class GetGroupEventsTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Events_For_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        group.GroupUsers.Add(groupUser);

        var evt1 = TestDataFactory.CreateEvent(
            "e1", group.Id, user.Id, "Event 1", null, null, DateTime.UtcNow);
        var evt2 = TestDataFactory.CreateEvent(
            "e2", group.Id, user.Id, "Event 2", null, null, DateTime.UtcNow.AddHours(1));
        dbContext.Events.AddRange(evt1, evt2);
        await dbContext.SaveChangesAsync();

        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<GetGroupEvents>.Instance;

        var result = await GetGroupEvents.Handle(
            group.Id, dbContext, claimsPrincipal, httpContext, logger, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<EventResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<EventResponseDto>>>;
        ok!.Value!.Data.Should().HaveCount(2);
        ok.Value.Data.Select(e => e.Id).Should().Contain(["e1", "e2"]);
    }

    [Fact]
    public async Task Handle_Should_Return_Forbidden_If_Not_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<GetGroupEvents>.Instance;

        var result = await GetGroupEvents.Handle(
            group.Id, dbContext, claimsPrincipal, httpContext, logger, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();    
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_If_Group_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<GetGroupEvents>.Instance;

        var result = await GetGroupEvents.Handle(
            "nonexistent-group", dbContext, claimsPrincipal, httpContext, logger, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_If_User_Not_LoggedIn()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var httpContext = new DefaultHttpContext();
        var logger = NullLogger<GetGroupEvents>.Instance;

        var result = await GetGroupEvents.Handle(
            "any-group", dbContext, new ClaimsPrincipal(), httpContext, logger, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }
}