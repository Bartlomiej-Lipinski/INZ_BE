using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Recommendations.Comments;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Recommendations.Comments;

public class GetRecommendationCommentsTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Comments_For_Recommendation()
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
        dbContext.Recommendations.Add(recommendation);

        var comment1 = TestDataFactory.CreateRecommendationComment(
            "c1", recommendation.Id, user.Id, "First comment", DateTime.UtcNow);
        var comment2 = TestDataFactory.CreateRecommendationComment(
            "c2", recommendation.Id, user.Id, "Second comment", DateTime.UtcNow);
        dbContext.RecommendationComments.AddRange(comment1, comment2);

        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<GetRecommendationComments>.Instance;

        var result = await GetRecommendationComments.Handle(
            recommendation.Id,
            dbContext,
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults
            .Ok<ApiResponse<List<GetRecommendationComments.CommentResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults
            .Ok<ApiResponse<List<GetRecommendationComments.CommentResponseDto>>>;
        ok!.Value!.Data.Should().HaveCount(2);
        ok.Value.Data.First().Content.Should().Be("First comment");
    }
}