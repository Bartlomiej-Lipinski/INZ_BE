using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class AcceptUserJoinRequestTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Request_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto(
                "group1", "user1"), dbContext, CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value?.Success.Should().BeFalse();
        notFound.Value?.Message.Should().Be("Join request not found.");
    }
    
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Request_Is_Not_Pending()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var groupUser = TestDataFactory.CreateGroupUser("group1", "user1");

        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto(
                groupUser.GroupId, groupUser.UserId), dbContext, CancellationToken.None);
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value?.Success.Should().BeFalse();
        badRequest.Value?.Message.Should().Be("Join request is not pending.");
    }
    
    [Fact]
    public async Task Handle_Should_Accept_Request_When_It_Is_Pending()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var groupUser =
            TestDataFactory.CreateGroupUser("group1", "user1", false, AcceptanceStatus.Pending);

        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await AcceptUserJoinRequest.Handle(
            TestDataFactory.CreateAcceptUserJoinRequestDto(
                groupUser.GroupId, groupUser.UserId), dbContext, CancellationToken.None);
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data.Should().Be("Join request accepted successfully.");
        
        var updatedGroupUser = await dbContext.GroupUsers.FirstAsync();
        updatedGroupUser.AcceptanceStatus.Should().Be(AcceptanceStatus.Accepted);
    }
}