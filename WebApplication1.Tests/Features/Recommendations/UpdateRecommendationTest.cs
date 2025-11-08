using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Recommendations;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Recommendations;

public class UpdateRecommendationTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Recommendation_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await UpdateRecommendation.Handle(
            group.Id,
            "nonexistent",
            TestDataFactory.CreateRecommendationRequestDto("Title", "Content"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateRecommendation>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Author()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var author = TestDataFactory.CreateUser("author", "AuthorUser");
        var otherUser = TestDataFactory.CreateUser("other", "OtherUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUserAuthor = TestDataFactory.CreateGroupUser(author.Id, group.Id);
        var groupUser = TestDataFactory.CreateGroupUser(otherUser.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.AddRange(author, otherUser);
        dbContext.GroupUsers.AddRange(groupUserAuthor, groupUser);

        var recommendation = TestDataFactory.CreateRecommendation(
            "rec1", group.Id, author.Id, "Old title", "Old content", DateTime.UtcNow);
        recommendation.GroupId = "g1";
        dbContext.Recommendations.Add(recommendation);

        await dbContext.SaveChangesAsync();
        
        var result = await UpdateRecommendation.Handle(
            group.Id,
            "rec1",
            TestDataFactory.CreateRecommendationRequestDto("Hacked!", "Evil update"),
            dbContext,
            CreateClaimsPrincipal(otherUser.Id),
            CreateHttpContext(otherUser.Id),
            NullLogger<UpdateRecommendation>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
    
    [Fact]
    public async Task Handle_Should_Update_Recommendation_Successfully()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);

        var recommendation = TestDataFactory.CreateRecommendation(
            "rec1", group.Id, user.Id, "Old title", "Old content", DateTime.UtcNow);
        recommendation.GroupId = "g1";
        dbContext.Recommendations.Add(recommendation);

        await dbContext.SaveChangesAsync();
        
        var result = await UpdateRecommendation.Handle(
            group.Id,
            "rec1",
            TestDataFactory.CreateRecommendationRequestDto(
                "Updated title",
                "Updated content", 
                "Books", 
                "https://example.com/image.jpg", 
                "https://example.com"
            ),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateRecommendation>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        var updated = await dbContext.Recommendations.FirstAsync(r => r.Id == "rec1");
        updated.Title.Should().Be("Updated title");
        updated.Content.Should().Be("Updated content");
        updated.Category.Should().Be("Books");
        updated.ImageUrl.Should().Be("https://example.com/image.jpg");
        updated.LinkUrl.Should().Be("https://example.com");
        updated.UpdatedAt.Should().NotBeNull();
    }
}