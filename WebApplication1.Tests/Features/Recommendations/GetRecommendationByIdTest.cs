using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Recommendations;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Recommendations;

public class GetRecommendationByIdTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Recommendation_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var logger = NullLogger<GetRecommendationById>.Instance;
        var claimsPrincipal = CreateClaimsPrincipal("test");

        var result = await GetRecommendationById.Handle(
            "nonexistent",
            dbContext,
            claimsPrincipal,
            httpContext,
            logger,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Recommendation_With_Comments_And_Reactions()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Groups.Add(group);
        
        var member = TestDataFactory.CreateGroupUser(group.Id, user.Id);
        dbContext.GroupUsers.Add(member);

        var recommendation = TestDataFactory.CreateRecommendation(
            "r1",
            group.Id,
            user.Id,
            "Test Recommendation",
            "Test content",
            DateTime.UtcNow
        );
        dbContext.Recommendations.Add(recommendation);
        
        var comment = TestDataFactory
            .CreateComment("c1", recommendation.Id, "Recommendation", user.Id, "Nice!", DateTime.UtcNow);
        dbContext.Comments.Add(comment);

        var reaction = TestDataFactory.CreateReaction(recommendation.Id, "Recommendation", user.Id);
        dbContext.Reactions.Add(reaction);

        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<GetRecommendationById>.Instance;
        var claimsPrincipal = CreateClaimsPrincipal(user.Id);


        var result = await GetRecommendationById.Handle(
            recommendation.Id,
            dbContext,
            claimsPrincipal,
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<GetRecommendationById.RecommendationResponseDto>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<GetRecommendationById.RecommendationResponseDto>>;
        okResult!.Value!.Data.Should().NotBeNull();
        okResult.Value.Data!.Id.Should().Be("r1");
        okResult.Value.Data.Title.Should().Be("Test Recommendation");
        okResult.Value.Data.Comments.Should().HaveCount(1);
        okResult.Value.Data.Reactions.Should().HaveCount(1);

        var commentDto = okResult.Value.Data.Comments.First();
        commentDto.Id.Should().Be("c1");
        commentDto.Content.Should().Be("Nice!");
        commentDto.UserId.Should().Be("u1");

        var reactionDto = okResult.Value.Data.Reactions.First();
        reactionDto.UserId.Should().Be("u1");
    }
}