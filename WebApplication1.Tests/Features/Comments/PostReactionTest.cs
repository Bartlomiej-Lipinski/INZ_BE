using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Comments;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Comments;

public class PostReactionTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Add_Reaction_When_Not_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Title", "Content", DateTime.UtcNow);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Recommendations.Add(target);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        var logger = NullLogger<PostReaction>.Instance;

        var result = await PostReaction.Handle(
            target.Id,
            dbContext,
            claimsPrincipal,
            httpContext,
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.Reactions.CountAsync()).Should().Be(1);
        var reaction = await dbContext.Reactions.FirstAsync();
        reaction.UserId.Should().Be("u1");
        reaction.TargetId.Should().Be("r1");
    }
    
    [Fact]
    public async Task Handle_Should_Remove_Reaction_When_Already_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Title", "Content", DateTime.UtcNow);

        var existingReaction = TestDataFactory.CreateReaction(target.Id, user.Id);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Recommendations.Add(target);
        dbContext.Reactions.Add(existingReaction);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        var logger = NullLogger<PostReaction>.Instance;

        var result = await PostReaction.Handle(
            target.Id,
            dbContext,
            claimsPrincipal,
            httpContext,
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.Reactions.CountAsync()).Should().Be(0);
    }
    
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Target_Not_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext("u1");
        var logger = NullLogger<PostReaction>.Instance;

        var result = await PostReaction.Handle(
            "nonexistent",
            dbContext,
            CreateClaimsPrincipal("u1"),
            httpContext,
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_No_User()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var logger = NullLogger<PostReaction>.Instance;

        var result = await PostReaction.Handle(
            "r1",
            dbContext,
            CreateClaimsPrincipal(),
            httpContext,
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Not_GroupMember()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "outsider");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, "u2", "Title", "Content", DateTime.UtcNow);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.Recommendations.Add(target);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var claimsPrincipal = CreateClaimsPrincipal(user.Id);
        var logger = NullLogger<PostReaction>.Instance;

        var result = await PostReaction.Handle(
            target.Id,
            dbContext,
            claimsPrincipal,
            httpContext,
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
}