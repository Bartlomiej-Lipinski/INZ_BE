using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Comments;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Comments;

public class PostReactionTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Add_Reaction_When_Not_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Title", "Content", DateTime.UtcNow);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Recommendations.Add(target);
        await dbContext.SaveChangesAsync();
        
        var result = await PostReaction.Handle(
            group.Id,
            target.Id,
            "Recommendation",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostReaction>.Instance,
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
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Title", "Content", DateTime.UtcNow);

        var existingReaction = TestDataFactory.CreateReaction(group.Id, target.Id, EntityType.Recommendation, user.Id);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Recommendations.Add(target);
        dbContext.Reactions.Add(existingReaction);
        await dbContext.SaveChangesAsync();
        
        var result = await PostReaction.Handle(
            group.Id,
            target.Id,
            "Recommendation",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostReaction>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.Reactions.CountAsync()).Should().Be(0);
    }
    
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Target_Not_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await PostReaction.Handle(
            "g1",
            "nonexistent",
            "Recommendation",
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<PostReaction>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}