using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Recommendations.Comments;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Recommendations.Comments;

public class DeleteRecommendationCommentTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Logged_In()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var logger = NullLogger<DeleteRecommendationComment>.Instance;

        var result = await DeleteRecommendationComment.Handle(
            "r1", 
            "c1",
            dbContext,
            CreateClaimsPrincipal(),
            httpContext,
            logger,
            CancellationToken.None);

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
        var logger = NullLogger<DeleteRecommendationComment>.Instance;

        var result = await DeleteRecommendationComment.Handle(
            "r1",
            "c1",
            dbContext, 
            CreateClaimsPrincipal(user.Id),
            httpContext,
            logger, 
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Allow_Author_To_Delete_Their_Comment()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "commentAuthor");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var recommendation = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Test", "Content", DateTime.UtcNow);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Recommendations.Add(recommendation);
        
        var comment = TestDataFactory.CreateRecommendationComment(
            "c1", recommendation.Id, user.Id, "Text", DateTime.UtcNow);
        dbContext.RecommendationComments.Add(comment);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<DeleteRecommendationComment>.Instance;

        var result = await DeleteRecommendationComment.Handle(
            recommendation.Id, 
            comment.Id, 
            dbContext, 
            CreateClaimsPrincipal(user.Id), 
            httpContext, 
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.RecommendationComments.AnyAsync(c => c.Id == comment.Id)).Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_Should_Allow_Recommendation_Author_To_Delete_Comment()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var owner = TestDataFactory.CreateUser("owner", "ownerUser");
        var commenter = TestDataFactory.CreateUser("commenter", "commentUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUserOwner = TestDataFactory.CreateGroupUser(owner.Id, group.Id);
        var groupUserCommenter = TestDataFactory.CreateGroupUser(commenter.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.AddRange(owner, commenter);
        dbContext.GroupUsers.AddRange(groupUserOwner, groupUserCommenter);

        var recommendation = TestDataFactory.CreateRecommendation(
            "r1", group.Id, owner.Id, "Test", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(recommendation);

        var comment = TestDataFactory.CreateRecommendationComment(
            "c1", recommendation.Id, commenter.Id, "Text", DateTime.UtcNow);
        dbContext.RecommendationComments.Add(comment);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(owner.Id);
        var logger = NullLogger<DeleteRecommendationComment>.Instance;

        var result = await DeleteRecommendationComment.Handle(
            recommendation.Id, 
            comment.Id, 
            dbContext, 
            CreateClaimsPrincipal(owner.Id),
            httpContext,
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.RecommendationComments.AnyAsync(c => c.Id == comment.Id)).Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Allowed()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var owner = TestDataFactory.CreateUser("owner", "ownerUser");
        var commenter = TestDataFactory.CreateUser("commenter", "commentUser");
        var other = TestDataFactory.CreateUser("other", "otherUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUserOwner = TestDataFactory.CreateGroupUser(owner.Id, group.Id);
        var groupUserCommenter = TestDataFactory.CreateGroupUser(commenter.Id, group.Id);
        var groupUserOther = TestDataFactory.CreateGroupUser(other.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.AddRange(owner, commenter, other);
        dbContext.GroupUsers.AddRange(groupUserOwner, groupUserCommenter, groupUserOther);
        
        var recommendation = TestDataFactory.CreateRecommendation(
            "r1", group.Id, owner.Id, "Test", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(recommendation);

        var comment = TestDataFactory.CreateRecommendationComment(
            "c1", recommendation.Id, commenter.Id, "Text", DateTime.UtcNow);
        dbContext.RecommendationComments.Add(comment);
        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(other.Id);
        var logger = NullLogger<DeleteRecommendationComment>.Instance;

        var result = await DeleteRecommendationComment.Handle(
            recommendation.Id,
            comment.Id, 
            dbContext, 
            CreateClaimsPrincipal(other.Id),
            httpContext, 
            logger, 
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
        (await dbContext.RecommendationComments.AnyAsync(c => c.Id == comment.Id)).Should().BeTrue();
    }
}