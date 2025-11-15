using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Comments;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Comments;

public class PostCommentTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Target_Not_Exists()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await PostComment.Handle(
            group.Id,
            "nonexistent",
            TestDataFactory.CreateCommentRequestDto("Hello!"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostComment>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Create_Comment_When_User_Is_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);

        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Title", "Content", DateTime.UtcNow);
        target.GroupId = "g1";
        dbContext.Recommendations.Add(target);

        var membership = TestDataFactory.CreateGroupUser("g1", "u1");
        dbContext.GroupUsers.Add(membership);

        await dbContext.SaveChangesAsync();
        
        var result = await PostComment.Handle(
            group.Id,
            "r1",
            TestDataFactory.CreateCommentRequestDto("Super!"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostComment>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Ok<ApiResponse<string>>>();
        (await dbContext.Comments.CountAsync()).Should().Be(1);
        var comment = await dbContext.Comments.FirstAsync();
        comment.Content.Should().Be("Super!");
        comment.UserId.Should().Be("u1");
        comment.TargetId.Should().Be("r1");
    }
}