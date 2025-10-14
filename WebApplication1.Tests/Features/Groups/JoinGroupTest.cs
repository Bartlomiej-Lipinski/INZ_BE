using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Tests.Features.Groups;

public class JoinGroupTest : TestBase
{
    [Fact]
    public async Task Handle_ShouldAddUserToGroup_WhenCodeIsValid()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF");
        var user = TestDataFactory.CreateUser(id: "user1");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        var httpContext = CreateHttpContextWithUser(user.Id);
        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        
        await GenerateCodeToJoinGroup
            .Handle("g1", dbContext, httpContext, NullLogger<GenerateCodeToJoinGroup>.Instance, CancellationToken.None);
        await JoinGroup.Handle(
            TestDataFactory.CreateJoinGroupRequest(group.Code),
            dbContext,
            claimsPrincipal,
            httpContext,
            CancellationToken.None
        );
        
        var groupUser = await dbContext.GroupUsers.FirstOrDefaultAsync(gu => gu.GroupId == "g1" && gu.UserId == "user1");
        groupUser.Should().NotBeNull();
        groupUser.IsAdmin.Should().BeFalse();
        groupUser.AcceptanceStatus.Should().Be(AcceptanceStatus.Pending);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser(id: "user1");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var httpContext = CreateHttpContextWithUser(user.Id);
        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        
        var result = await JoinGroup.Handle(
            TestDataFactory.CreateJoinGroupRequest("INVALIDCODE"),
            dbContext,
            claimsPrincipal,
            httpContext,
            CancellationToken.None
        );
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<Shared.Responses.ApiResponse<string>>>();
        
        var notFoundResult =
            result as Microsoft.AspNetCore.Http.HttpResults.NotFound<Shared.Responses.ApiResponse<string>>;
        notFoundResult!.Value?.Success.Should().BeFalse();
        notFoundResult.Value?.Message.Should().Be("Group not found or code is invalid.");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenCodeIsExpired()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString()); 
        var httpContext = CreateHttpContextWithUser();
        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        await GenerateCodeToJoinGroup.Handle("g1", dbContext, new DefaultHttpContext(), 
            NullLogger<GenerateCodeToJoinGroup>.Instance, CancellationToken.None);
        
        var existingGroup = await dbContext.Groups.FirstOrDefaultAsync(g => g.Id == "g1");
        existingGroup!.CodeExpirationTime = DateTime.UtcNow.AddMinutes(-1);
        dbContext.Groups.Update(existingGroup);
        await dbContext.SaveChangesAsync();
        var user1 = TestDataFactory.CreateUser(id: "user1");
        dbContext.Users.Add(user1);
        await dbContext.SaveChangesAsync();
        var claimsPrincipal = CreateClaimsPrincipal(user1.Id);
        
        var result = await JoinGroup.Handle(
            TestDataFactory.CreateJoinGroupRequest(existingGroup.Code),
            dbContext,
            claimsPrincipal,
            httpContext,
            CancellationToken.None
        );
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<Shared.Responses.ApiResponse<string>>>();
        
        var badRequestResult = 
            result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<Shared.Responses.ApiResponse<string>>;
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("The code has expired.");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenUserAlreadyInGroup()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF");
        var user = TestDataFactory.CreateUser(id: "user1");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        var httpContext = CreateHttpContextWithUser(user.Id);
        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        
        await GenerateCodeToJoinGroup.Handle("g1", dbContext, httpContext, 
            NullLogger<GenerateCodeToJoinGroup>.Instance, CancellationToken.None);
        await JoinGroup.Handle(
            TestDataFactory.CreateJoinGroupRequest(group.Code), 
            dbContext,
            claimsPrincipal,
            httpContext,
            CancellationToken.None
        );
        
        var result = await JoinGroup.Handle(
            TestDataFactory.CreateJoinGroupRequest(group.Code), 
            dbContext,
            claimsPrincipal,
            httpContext,
            CancellationToken.None
        );
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<Shared.Responses.ApiResponse<string>>>();
        
        var badRequestResult = 
            result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<Shared.Responses.ApiResponse<string>>;
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("You are already a member of this group.");
    }
}