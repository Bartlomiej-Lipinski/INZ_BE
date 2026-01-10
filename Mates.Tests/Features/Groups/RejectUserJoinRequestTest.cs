using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Groups.JoinGroupFeatures;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Groups;

public class RejectUserJoinRequestTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Request_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var result = await RejectUserJoinRequest.Handle(
            "g1",
            TestDataFactory.CreateRejectUserJoinRequestDto("u1"), 
            dbContext, 
            CreateClaimsPrincipal("u1"),
            NullLogger<RejectUserJoinRequest>.Instance,
            CreateHttpContext("u1"), 
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Request_Is_Not_Pending()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Groups.Add(group);
        
        var adminGroupUser = TestDataFactory.CreateGroupUser("admin1", "g1", true);
        dbContext.GroupUsers.Add(adminGroupUser);
        
        var acceptedUser = TestDataFactory.CreateGroupUser("u1", "g1");
        dbContext.GroupUsers.Add(acceptedUser);
        
        await dbContext.SaveChangesAsync();
        
        var result = await RejectUserJoinRequest.Handle(
            "g1",
            TestDataFactory.CreateRejectUserJoinRequestDto("u1"),
            dbContext, 
            CreateClaimsPrincipal("admin1"), 
            NullLogger<RejectUserJoinRequest>.Instance, 
            CreateHttpContext(), 
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value!.Success.Should().BeFalse();
        badRequest.Value.Message.Should().Be("Join request is not pending.");
        badRequest.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Remove_GroupUser_When_Request_Is_Pending()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Groups.Add(group);
        
        var adminGroupUser = TestDataFactory.CreateGroupUser("admin1", "g1", true);
        dbContext.GroupUsers.Add(adminGroupUser);
        
        var pendingUser = TestDataFactory.CreateGroupUser("u1", "g1", false, AcceptanceStatus.Pending);
        dbContext.GroupUsers.Add(pendingUser);
        
        await dbContext.SaveChangesAsync();

        var result = await RejectUserJoinRequest.Handle(
            "g1",
            TestDataFactory.CreateRejectUserJoinRequestDto("u1"),
            dbContext, 
            CreateClaimsPrincipal("admin1"), 
            NullLogger<RejectUserJoinRequest>.Instance, 
            CreateHttpContext("g1"), 
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().Be("Join request rejected successfully.");
        
        var stillExists = await dbContext.GroupUsers.AnyAsync(gu => gu.GroupId == "g1" && gu.UserId == "u1");
        stillExists.Should().BeFalse();
    }
}