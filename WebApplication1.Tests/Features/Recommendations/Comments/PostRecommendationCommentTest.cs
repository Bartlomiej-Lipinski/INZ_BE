using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Recommendations.Comments;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Recommendations.Comments;

public class PostRecommendationCommentTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var logger = NullLogger<PostRecommendationComment>.Instance;

        var result = await PostRecommendationComment.Handle(
            "rec1",
            TestDataFactory.CreateCommentRequestDto("Hello!"),
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
        var logger = NullLogger<PostRecommendationComment>.Instance;

        var result = await PostRecommendationComment.Handle(
            "nonexistent",
            TestDataFactory.CreateCommentRequestDto("Hello!"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Not_In_Group()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "user");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);

        var recommendation = TestDataFactory.CreateRecommendation(
            "r1", group.Id, "author", "Title", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(recommendation);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<PostRecommendationComment>.Instance;

        var result = await PostRecommendationComment.Handle(
            "r1",
            TestDataFactory.CreateCommentRequestDto("Hello!"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
    
    [Fact]
    public async Task Handle_Should_Create_Comment_When_User_Is_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);

        var recommendation = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Title", "Content", DateTime.UtcNow);
        recommendation.GroupId = "g1";
        dbContext.Recommendations.Add(recommendation);

        var membership = TestDataFactory.CreateGroupUser("g1", "u1");
        dbContext.GroupUsers.Add(membership);

        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<PostRecommendationComment>.Instance;

        var result = await PostRecommendationComment.Handle(
            "r1",
            new PostRecommendationComment.CommentRequestDto { Content = "Super!" },
            dbContext,
            CreateClaimsPrincipal(user.Id),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.RecommendationComments.CountAsync()).Should().Be(1);
        var comment = await dbContext.RecommendationComments.FirstAsync();
        comment.Content.Should().Be("Super!");
        comment.UserId.Should().Be("u1");
        comment.RecommendationId.Should().Be("r1");
    }
}