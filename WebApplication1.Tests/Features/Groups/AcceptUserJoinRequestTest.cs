using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class AcceptUserJoinRequestTest : TestBase
{
    private ClaimsPrincipal CreateUser(string userId)
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId)
        ], "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private HttpContext CreateMockHttpContext()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace-id"
        };
        return context;
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Request_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateUser("admin1");
        var httpContext = CreateMockHttpContext();
        var mockLogger = new Mock<ILogger<AcceptUserJoinRequest>>();
        var adminGroupUser = TestDataFactory.CreateGroupUser("admin1", "group1", true);
        dbContext.GroupUsers.Add(adminGroupUser);
        await dbContext.SaveChangesAsync();

        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto("group1", "user1"), 
            dbContext, user, CancellationToken.None, httpContext, mockLogger.Object);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value?.Success.Should().BeFalse();
        notFound.Value?.Message.Should().Be("Join request not found.");
        notFound.Value?.TraceId.Should().Be("test-trace-id");
    }
    
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Request_Is_Not_Pending()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateUser("admin1");
        var httpContext = CreateMockHttpContext();
        var mockLogger = new Mock<ILogger<AcceptUserJoinRequest>>();
        var adminGroupUser = TestDataFactory.CreateGroupUser("admin1", "group1", true);
        dbContext.GroupUsers.Add(adminGroupUser);
        
        var groupUser = TestDataFactory.CreateGroupUser("user1", "group1");
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto("group1", "user1"), 
            dbContext, user, CancellationToken.None, httpContext, mockLogger.Object);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value?.Success.Should().BeFalse();
        badRequest.Value?.Message.Should().Be("Join request is not pending.");
        badRequest.Value?.TraceId.Should().Be("test-trace-id");
    }
    
    [Fact]
    public async Task Handle_Should_Accept_Request_When_It_Is_Pending()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateUser("admin1");
        var httpContext = CreateMockHttpContext();
        var mockLogger = new Mock<ILogger<AcceptUserJoinRequest>>();

        var adminGroupUser = TestDataFactory.CreateGroupUser("admin1", "group1", true);
        dbContext.GroupUsers.Add(adminGroupUser);
        
        var groupUser = TestDataFactory.CreateGroupUser("user1", "group1", false, AcceptanceStatus.Pending);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto("group1", "user1"), 
            dbContext, user, CancellationToken.None, httpContext, mockLogger.Object);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Message.Should().Be("Join request accepted successfully.");
        okResult.Value?.TraceId.Should().Be("test-trace-id");
        
        var updatedGroupUser = await dbContext.GroupUsers
            .FirstAsync(gu => gu.GroupId == "group1" && gu.UserId == "user1");
        updatedGroupUser.AcceptanceStatus.Should().Be(AcceptanceStatus.Accepted);
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Is_Null()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateMockHttpContext();
        var mockLogger = new Mock<ILogger<AcceptUserJoinRequest>>();

        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto("group1", "user1"), 
            dbContext, null, CancellationToken.None, httpContext, mockLogger.Object);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_User_Is_Not_Admin()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateUser("user2");
        var httpContext = CreateMockHttpContext();
        var mockLogger = new Mock<ILogger<AcceptUserJoinRequest>>();

        var nonAdminUser = TestDataFactory.CreateGroupUser("user2", "group1");
        dbContext.GroupUsers.Add(nonAdminUser);

        var pendingUser = TestDataFactory.CreateGroupUser("user1", "group1", false, AcceptanceStatus.Pending);
        dbContext.GroupUsers.Add(pendingUser);
        await dbContext.SaveChangesAsync();

        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto("group1", "user1"), 
            dbContext, user, CancellationToken.None, httpContext, mockLogger.Object);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value?.Message.Should().Be("Only group admin can accept join requests.");
        badRequest.Value?.TraceId.Should().Be("test-trace-id");
    }
}