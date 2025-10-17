using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Recommendations;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Recommendations;

public class DeleteRecommendationTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var logger = NullLogger<DeleteRecommendation>.Instance;

        var result = await DeleteRecommendation.Handle(
            "rec1",
            dbContext,
            CreateClaimsPrincipal(),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Recommendation_Not_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<DeleteRecommendation>.Instance;

        var result = await DeleteRecommendation.Handle(
            "nonexistent",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Author()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var author = TestDataFactory.CreateUser("author", "AuthorUser");
        var other = TestDataFactory.CreateUser("other", "OtherUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUserAuthor = TestDataFactory.CreateGroupUser(author.Id, group.Id);
        var groupUser = TestDataFactory.CreateGroupUser(other.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.AddRange(author, other);
        dbContext.GroupUsers.AddRange(groupUserAuthor, groupUser);

        var recommendation = TestDataFactory.CreateRecommendation(
            "rec1", group.Id, author.Id, "Title", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(recommendation);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(other.Id);
        var logger = NullLogger<DeleteRecommendation>.Instance;

        var result = await DeleteRecommendation.Handle(
            "rec1",
            dbContext,
            CreateClaimsPrincipal(other.Id),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
    
    [Fact]
    public async Task Handle_Should_Delete_Recommendation_Comments_And_Reactions()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);

        var recommendation = TestDataFactory.CreateRecommendation(
            "rec1", group.Id, user.Id, "Title", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(recommendation);

        var comment = TestDataFactory.CreateComment(
            "c1", "rec1", "Recommendation", user.Id, "Comment", DateTime.UtcNow);
        var reaction = TestDataFactory.CreateReaction("rec1", "Recommendation", user.Id);
        dbContext.Comments.Add(comment);
        dbContext.Reactions.Add(reaction);

        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<DeleteRecommendation>.Instance;

        var result = await DeleteRecommendation.Handle(
            "rec1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        (await dbContext.Recommendations.CountAsync()).Should().Be(0);
        (await dbContext.Comments.CountAsync()).Should().Be(0);
        (await dbContext.Reactions.CountAsync()).Should().Be(0);
    }
}