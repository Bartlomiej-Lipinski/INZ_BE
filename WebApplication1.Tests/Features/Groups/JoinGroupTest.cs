using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Tests.Features.Groups;

public class JoinGroupTest : TestBase
{
    [Fact]
    public async Task Handle_ShouldAddUserToGroup_WhenCodeIsValid()
    {
        var dbContext = GetInMemoryDbContext();
        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF");
        var user = TestDataFactory.CreateUser(id: "user1");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        GenerateCodeToJoinGroup.Handle("g1", dbContext, CancellationToken.None).Wait();
        JoinGroup.Handle( new JoinGroup.JoinGroupRequest(group.Code, "user1"), dbContext, CancellationToken.None).Wait();
        var groupUser = await dbContext.GroupUsers.FirstOrDefaultAsync(gu => gu.GroupId == "g1" && gu.UserId == "user1");
        groupUser.Should().NotBeNull();
        groupUser!.IsAdmin.Should().BeFalse();
        groupUser.AcceptanceStatus.Should().Be(AcceptanceStatus.Pending);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        var dbContext = GetInMemoryDbContext();
        var result = await JoinGroup.Handle(new JoinGroup.JoinGroupRequest("INVALIDCODE", "user1"), dbContext, CancellationToken.None);
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<WebApplication1.Shared.Responses.ApiResponse<string>>>();
        var notFoundResult = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<WebApplication1.Shared.Responses.ApiResponse<string>>;
        notFoundResult!.Value?.Success.Should().BeFalse();
        notFoundResult.Value?.Message.Should().Be("Group not found or code is invalid.");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenCodeIsExpired()
    {
        var dbContext = GetInMemoryDbContext();
        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        GenerateCodeToJoinGroup.Handle("g1", dbContext, CancellationToken.None).Wait();
        var existingGroup = await dbContext.Groups.FirstOrDefaultAsync(g => g.Id == "g1");
        existingGroup!.CodeExpirationTime = DateTime.UtcNow.AddMinutes(-1);
        dbContext.Groups.Update(existingGroup);
        await dbContext.SaveChangesAsync();
        var result = await JoinGroup.Handle(new JoinGroup.JoinGroupRequest(existingGroup.Code, "user1"), dbContext, CancellationToken.None);
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<WebApplication1.Shared.Responses.ApiResponse<string>>>();
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<WebApplication1.Shared.Responses.ApiResponse<string>>;
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("The code has expired.");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenUserAlreadyInGroup()
    {
        var dbContext = GetInMemoryDbContext();
        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF");
        var user = TestDataFactory.CreateUser(id: "user1");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        GenerateCodeToJoinGroup.Handle("g1", dbContext, CancellationToken.None).Wait();
        JoinGroup.Handle(new JoinGroup.JoinGroupRequest(group.Code, "user1"), dbContext, CancellationToken.None).Wait();
        var result = await JoinGroup.Handle(new JoinGroup.JoinGroupRequest(group.Code, "user1"), dbContext, CancellationToken.None);
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<WebApplication1.Shared.Responses.ApiResponse<string>>>();
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<WebApplication1.Shared.Responses.ApiResponse<string>>;
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("You are already a member of this group.");
    }
}