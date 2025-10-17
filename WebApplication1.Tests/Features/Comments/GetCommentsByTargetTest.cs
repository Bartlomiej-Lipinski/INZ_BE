using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Comments;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Comments;

public class GetCommentsByTargetTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Comments_For_Target()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
    
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);

        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Title", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(target);

        var comment1 = TestDataFactory.CreateComment(
            "c1", target.Id, "Recommendation", user.Id, "First comment", DateTime.UtcNow);
        var comment2 = TestDataFactory.CreateComment(
            "c2", target.Id, "Recommendation", user.Id, "Second comment", DateTime.UtcNow);
        dbContext.Comments.AddRange(comment1, comment2);

        await dbContext.SaveChangesAsync();

        var httpContext = CreateHttpContext(user.Id);
        var logger = NullLogger<GetCommentsByTarget>.Instance;

        var result = await GetCommentsByTarget.Handle(
            target.Id,
            dbContext,
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults
            .Ok<ApiResponse<List<GetCommentsByTarget.CommentResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults
            .Ok<ApiResponse<List<GetCommentsByTarget.CommentResponseDto>>>;
        ok!.Value!.Data.Should().HaveCount(2);
        ok.Value.Data.Should().ContainSingle(c => c.Content == "First comment");
        ok.Value.Data.Should().ContainSingle(c => c.Content == "Second comment");
    }
}