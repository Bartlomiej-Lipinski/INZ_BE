using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Recommendations.Comments;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Recommendations.Comments;

public class UpdateRecommendationCommentTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Logged_In()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var logger = NullLogger<UpdateRecommendationComment>.Instance;

        var result = await UpdateRecommendationComment.Handle(
            "r1",
            "c1",
            TestDataFactory.CreateUpdateCommentRequestDto("Updated"),
            dbContext,
            CreateClaimsPrincipal(),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Comment_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<UpdateRecommendationComment>.Instance;

        var result = await UpdateRecommendationComment.Handle(
            "r1",
            "c1",
            TestDataFactory.CreateUpdateCommentRequestDto("Updated"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Update_Comment_Successfully()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);

        var recommendation = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Test", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(recommendation);

        var comment = TestDataFactory.CreateRecommendationComment(
            "c1", recommendation.Id, user.Id, "Old content", DateTime.UtcNow);
        dbContext.RecommendationComments.Add(comment);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<UpdateRecommendationComment>.Instance;

        var result = await UpdateRecommendationComment.Handle(
            recommendation.Id,
            comment.Id,
            TestDataFactory.CreateUpdateCommentRequestDto("Updated"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        var updated = await dbContext.RecommendationComments.FindAsync(comment.Id);
        updated!.Content.Should().Be("Updated");
    }
}