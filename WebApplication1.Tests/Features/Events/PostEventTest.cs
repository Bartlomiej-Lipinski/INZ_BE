using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Events;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Events;

public class PostEventTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Create_Event_When_Valid()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, "g1");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateEventRequestDto(
            "New Event", 
            DateTime.UtcNow.AddDays(1), 
            DateTime.UtcNow.AddDays(1).AddHours(2),
            "Event description", 
            "Online");

        var httpContext = CreateHttpContext();
        var claims = CreateClaimsPrincipal(user.Id);
        var logger = NullLogger<PostEvent>.Instance;

        var result = await PostEvent.Handle(
            group.Id, request, dbContext, claims, httpContext, logger, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<PostEvent.EventResponseDto>>>();

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<PostEvent.EventResponseDto>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data!.Title.Should().Be("New Event");

        var dbEvent = await dbContext.Events.FirstOrDefaultAsync();
        dbEvent.Should().NotBeNull();
        dbEvent.Title.Should().Be("New Event");
        dbEvent.GroupId.Should().Be("g1");
        dbEvent.UserId.Should().Be("u1");
    }
    
    [Fact]
    public async Task Handle_Should_Return_Forbidden_When_User_Not_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");  
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateEventRequestDto("Event", DateTime.UtcNow.AddDays(1));
        var httpContext = CreateHttpContext();
        var claims = CreateClaimsPrincipal(user.Id);
        var logger = NullLogger<PostEvent>.Instance;

        var result = await PostEvent.Handle(group.Id, request, dbContext, claims, httpContext, logger, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();    
    }
    
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Title_Missing()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");  
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, "g1");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateEventRequestDto("", DateTime.UtcNow.AddDays(1));
        var httpContext = CreateHttpContext();
        var claims = CreateClaimsPrincipal(user.Id);
        var logger = NullLogger<PostEvent>.Instance;

        var result = await PostEvent.Handle(
            group.Id, request, dbContext, claims, httpContext, logger, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }
}