using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Comments;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Comments;

public class DeleteCommentTest : TestBase
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
        
        var result = await DeleteComment.Handle(
            group.Id,
            "r1",
            "c1",
            dbContext, 
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteComment>.Instance, 
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Allow_Author_To_Delete_Their_Comment()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, user.Id, "Test", "Content", DateTime.UtcNow);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Recommendations.Add(target);
        
        var comment = TestDataFactory.CreateComment(
            "c1", group.Id, target.Id, EntityType.Recommendation, user.Id, "Text", DateTime.UtcNow);
        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();

        var result = await DeleteComment.Handle(
            group.Id,
            target.Id, 
            comment.Id, 
            dbContext, 
            CreateClaimsPrincipal(user.Id), 
            CreateHttpContext(user.Id), 
            NullLogger<DeleteComment>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.Comments.AnyAsync(c => c.Id == comment.Id)).Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_Should_Allow_Target_Author_To_Delete_Comment()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var owner = TestDataFactory.CreateUser("owner", "Test","User");
        var commenter = TestDataFactory.CreateUser("commenter", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUserOwner = TestDataFactory.CreateGroupUser(owner.Id, group.Id);
        var groupUserCommenter = TestDataFactory.CreateGroupUser(commenter.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.AddRange(owner, commenter);
        dbContext.GroupUsers.AddRange(groupUserOwner, groupUserCommenter);

        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, owner.Id, "Test", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(target);

        var comment = TestDataFactory.CreateComment(
            "c1", group.Id, target.Id, EntityType.Recommendation, commenter.Id, "Text", DateTime.UtcNow);
        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();

        var result = await DeleteComment.Handle(
            group.Id,
            target.Id, 
            comment.Id, 
            dbContext, 
            CreateClaimsPrincipal(owner.Id),
            CreateHttpContext(owner.Id),
            NullLogger<DeleteComment>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.Comments.AnyAsync(c => c.Id == comment.Id)).Should().BeFalse();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Allowed()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var owner = TestDataFactory.CreateUser("owner", "Test","User");
        var commenter = TestDataFactory.CreateUser("commenter", "Test","User");
        var other = TestDataFactory.CreateUser("other", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUserOwner = TestDataFactory.CreateGroupUser(owner.Id, group.Id);
        var groupUserCommenter = TestDataFactory.CreateGroupUser(commenter.Id, group.Id);
        var groupUserOther = TestDataFactory.CreateGroupUser(other.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.AddRange(owner, commenter, other);
        dbContext.GroupUsers.AddRange(groupUserOwner, groupUserCommenter, groupUserOther);
        
        var target = TestDataFactory.CreateRecommendation(
            "r1", group.Id, owner.Id, "Test", "Content", DateTime.UtcNow);
        dbContext.Recommendations.Add(target);

        var comment = TestDataFactory.CreateComment(
            "c1", group.Id, target.Id, EntityType.Recommendation, commenter.Id, "Text", DateTime.UtcNow);
        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();
        
        var result = await DeleteComment.Handle(
            group.Id,
            target.Id,
            comment.Id, 
            dbContext, 
            CreateClaimsPrincipal(other.Id),
            CreateHttpContext(other.Id), 
            NullLogger<DeleteComment>.Instance, 
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
        (await dbContext.Comments.AnyAsync(c => c.Id == comment.Id)).Should().BeTrue();
    }
}