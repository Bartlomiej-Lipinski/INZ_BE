using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Recommendations;
using Mates.Features.Recommendations.Dtos;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Recommendations;

public class GetRecommendationByIdTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Recommendation_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await GetRecommendationById.Handle(
            "g1",
            "nonexistent",
            dbContext,
            CreateClaimsPrincipal("test"),
            CreateHttpContext(),
            NullLogger<GetRecommendationById>.Instance,
            CancellationToken.None
        );
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Recommendation_With_Comments_And_Reactions()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);

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
            .CreateComment("c1", group.Id, recommendation.Id, EntityType.Recommendation, user.Id, "Nice!", DateTime.UtcNow);
        dbContext.Comments.Add(comment);

        var reaction = TestDataFactory.CreateReaction(group.Id, recommendation.Id, EntityType.Recommendation, user.Id);
        dbContext.Reactions.Add(reaction);
        await dbContext.SaveChangesAsync();
        
        var result = await GetRecommendationById.Handle(
            group.Id,
            recommendation.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetRecommendationById>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<RecommendationResponseDto>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<RecommendationResponseDto>>;
        okResult!.Value!.Data.Should().NotBeNull();
        okResult.Value.Data!.Id.Should().Be("r1");
        okResult.Value.Data.Title.Should().Be("Test Recommendation");
        okResult.Value.Data.Comments.Should().HaveCount(1);
        okResult.Value.Data.Reactions.Should().HaveCount(1);

        var commentDto = okResult.Value.Data.Comments.First();
        commentDto.Id.Should().Be("c1");
        commentDto.Content.Should().Be("Nice!");
        commentDto.User.Id.Should().Be("u1");

        var reactionDto = okResult.Value.Data.Reactions.First();
        reactionDto.Id.Should().Be("u1");
    }
}