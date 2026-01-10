using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Groups.JoinGroupFeatures;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Groups;

public class AcceptUserJoinRequestTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Request_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var adminGroupUser = TestDataFactory.CreateGroupUser("admin1", "group1", true);
        dbContext.GroupUsers.Add(adminGroupUser);
        await dbContext.SaveChangesAsync();

        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto("group1", "user1"), 
            dbContext,
            CreateClaimsPrincipal("admin1"),
            CreateHttpContext("admin1"),
            NullLogger<AcceptUserJoinRequest>.Instance,
            CancellationToken.None
        );

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
        var adminGroupUser = TestDataFactory.CreateGroupUser("admin1", "group1", true);
        dbContext.GroupUsers.Add(adminGroupUser);
        
        var groupUser = TestDataFactory.CreateGroupUser("user1", "group1");
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto("group1", "user1"), 
            dbContext,
            CreateClaimsPrincipal("admin1"), 
            CreateHttpContext("admin1"), 
            NullLogger<AcceptUserJoinRequest>.Instance, 
            CancellationToken.None
        );

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
        var adminGroupUser = TestDataFactory.CreateGroupUser("admin1", "group1", true);
        dbContext.GroupUsers.Add(adminGroupUser);
        
        var groupUser = TestDataFactory.CreateGroupUser("user1", "group1", false, AcceptanceStatus.Pending);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto("group1", "user1"), 
            dbContext, 
            CreateClaimsPrincipal("admin1"),
            CreateHttpContext("admin1"), 
            NullLogger<AcceptUserJoinRequest>.Instance, 
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data.Should().Be("Join request accepted successfully.");
        okResult.Value?.TraceId.Should().Be("test-trace-id");
        
        var updatedGroupUser = await dbContext.GroupUsers
            .FirstAsync(gu => gu.GroupId == "group1" && gu.UserId == "user1");
        updatedGroupUser.AcceptanceStatus.Should().Be(AcceptanceStatus.Accepted);
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_User_Is_Not_Admin()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var nonAdminUser = TestDataFactory.CreateGroupUser("user2", "group1");
        dbContext.GroupUsers.Add(nonAdminUser);

        var pendingUser = TestDataFactory.CreateGroupUser("user1", "group1", false, AcceptanceStatus.Pending);
        dbContext.GroupUsers.Add(pendingUser);
        await dbContext.SaveChangesAsync();

        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto("group1", "user1"), 
            dbContext, 
            CreateClaimsPrincipal("user2"), 
            CreateHttpContext("user2"),
            NullLogger<AcceptUserJoinRequest>.Instance, 
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value?.Message.Should().Be("Only group admin can accept join requests.");
        badRequest.Value?.TraceId.Should().Be("test-trace-id");
    }
}