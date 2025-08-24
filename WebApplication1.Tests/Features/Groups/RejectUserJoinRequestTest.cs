using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class RejectUserJoinRequestTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Request_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var result = await RejectUserJoinRequest.Handle(
            TestDataFactory.CreateRejectUserJoinRequestDto("g1", "u1"), dbContext, CancellationToken.None);
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Request_Is_Not_Pending()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var groupUser = TestDataFactory.CreateGroupUser("g1", "u1");
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await RejectUserJoinRequest.Handle(
                TestDataFactory.CreateRejectUserJoinRequestDto(
                    groupUser.GroupId, groupUser.UserId), dbContext, CancellationToken.None);
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Remove_GroupUser_When_Request_Is_Pending()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var groupUser = TestDataFactory.CreateGroupUser("g1", "u1", false, AcceptanceStatus.Pending);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await RejectUserJoinRequest.Handle(
                TestDataFactory.CreateRejectUserJoinRequestDto(
                    groupUser.GroupId, groupUser.UserId), dbContext, CancellationToken.None);
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().Be("Join request rejected successfully.");
        
        var stillExists = await dbContext.GroupUsers.AnyAsync(gu => gu.GroupId == "g1" && gu.UserId == "u1");
        stillExists.Should().BeFalse();
    }
}