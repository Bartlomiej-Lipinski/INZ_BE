using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Recommendations;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Recommendations;

public class PostRecommendationTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Create_Recommendation_When_User_Is_Group_Member()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "testUser");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var dto = TestDataFactory.CreateRecommendationRequestDto(
            "Great Book", 
            "You should read 'Clean Code'.", 
            "Books");
        
        var result = await PostRecommendation.Handle(
            group.Id,
            dto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostRecommendation>.Instance,
            CancellationToken.None);
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().Be("Recommendation created successfully.");

        var created = await dbContext.Recommendations.FirstOrDefaultAsync();
        created.Should().NotBeNull();
        created.Title.Should().Be("Great Book");
        created.Content.Should().Contain("Clean Code");
        created.GroupId.Should().Be(group.Id);
        created.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Has_No_Claims()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var dto = TestDataFactory.CreateRecommendationRequestDto("Test", "Test content");

        var result = await PostRecommendation.Handle(
            "g1",
            dto,
            dbContext,
            CreateClaimsPrincipal(),
            CreateHttpContext(),
            NullLogger<PostRecommendation>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Missing_Title_Or_Content()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var dto = TestDataFactory.CreateRecommendationRequestDto("", "");

        var result = await PostRecommendation.Handle(
            "g1",
            dto,
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<PostRecommendation>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Member_Of_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        dbContext.Groups.Add(TestDataFactory.CreateGroup("g1", "Test Group"));
        await dbContext.SaveChangesAsync();
        
        var dto = TestDataFactory.CreateRecommendationRequestDto("Test", "Some text");

        var result = await PostRecommendation.Handle(
            "g1",
            dto,
            dbContext,
            CreateClaimsPrincipal("u2"),
            CreateHttpContext("u2"),
            NullLogger<PostRecommendation>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
}