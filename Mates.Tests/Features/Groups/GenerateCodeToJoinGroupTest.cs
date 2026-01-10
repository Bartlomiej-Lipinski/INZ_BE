using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Groups.JoinGroupFeatures;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Groups;

public class GenerateCodeToJoinGroupTest : TestBase
{
    [Fact]
    public async Task Handle_ShouldGenerateUniqueCode()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group", "#FFFFFF");
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await GenerateCodeToJoinGroup.Handle(
            "g1", 
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GenerateCodeToJoinGroup>.Instance, 
            CancellationToken.None
        );
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        
        var okResult = 
            result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data?.Should().Be($"New code generated successfully. The code is valid for 5 minutes. Code: {group.Code}");
        
        var updatedGroup = await dbContext.Groups.FindAsync("g1");
        updatedGroup.Should().NotBeNull();
        updatedGroup.CodeExpirationTime.Should().BeAfter(DateTime.UtcNow); 
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        var result = await GenerateCodeToJoinGroup.Handle(
            "non-existent", 
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GenerateCodeToJoinGroup>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
       
        var notFoundResult = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFoundResult!.Value?.Success.Should().BeFalse();
        notFoundResult.Value?.Message.Should().Be("Group not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenGroupIdIsEmpty()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        var result = await GenerateCodeToJoinGroup.Handle(
            "",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id), 
            NullLogger<GenerateCodeToJoinGroup>.Instance, 
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("Group ID cannot be null or empty.");
    }
}