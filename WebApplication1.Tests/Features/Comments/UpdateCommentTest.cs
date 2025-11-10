using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Comments;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Comments;

public class UpdateCommentTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Comment_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await UpdateComment.Handle(
            group.Id,
            "r1",
            "c1",
            TestDataFactory.CreateCommentRequestDto("Updated"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateComment>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Update_Comment_Successfully()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);

        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Test", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(target);

        var comment = TestDataFactory.CreateComment(
            "c1", target.Id, EntityType.Recommendation, user.Id, "Old content", DateTime.UtcNow);
        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();
        
        var result = await UpdateComment.Handle(
            group.Id,
            target.Id,
            comment.Id,
            TestDataFactory.CreateCommentRequestDto("Updated"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateComment>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        var updated = await dbContext.Comments.FindAsync(comment.Id);
        updated!.Content.Should().Be("Updated");
    }
}