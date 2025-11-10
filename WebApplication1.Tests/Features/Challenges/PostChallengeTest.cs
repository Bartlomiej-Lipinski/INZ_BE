using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Challenges;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Challenges;

public class PostChallengeTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Create_Challenge_Successfully()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("user1", "Test", "User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeRequestDto(
            "Test Challenge",
            "Do something",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            10,
            "steps"
        );

        var result = await PostChallenge.Handle(
            "group1",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<PostChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data.Should().NotBeNullOrEmpty();
        okResult.Value.Data.Should().Be("Challenge created successfully.");
        okResult.Value.TraceId.Should().Be("test-trace-id");

        var challengeInDb = await dbContext.Challenges.FirstOrDefaultAsync();
        challengeInDb.Should().NotBeNull();
        challengeInDb!.Name.Should().Be(request.Name);
        challengeInDb.Description.Should().Be(request.Description);
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_NameOrDescription_IsEmpty()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("user1", "Test", "User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeRequestDto(
            "",
            "",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            10,
            "steps"
        );

        var result = await PostChallenge.Handle(
            "group1",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<PostChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_EndDate_Before_StartDate()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("user1", "Test", "User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeRequestDto(
            "Test Challenge",
            "Description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1),
            10,
            "steps"
        );

        var result = await PostChallenge.Handle(
            "group1",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(),
            NullLogger<PostChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }
}