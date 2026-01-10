using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Challenges;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Challenges;

public class DeleteChallengeTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Delete_Challenge_Successfully_When_User_Is_Owner()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("user1", "Test", "User");
        dbContext.Users.Add(user);

        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "group1",
            user.Id, 
            "Run 10km",
            "Challenge",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1), 
            [], 
            "km",
            10
        );
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var result = await DeleteChallenge.Handle(
            "group1",
            "ch1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().Be("Challenge deleted successfully.");
        ok.Value.TraceId.Should().Be("test-trace-id");

        (await dbContext.Challenges.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Challenge_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await DeleteChallenge.Handle(
            "group1",
            "nonexistent",
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<DeleteChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value!.Success.Should().BeFalse();
        notFound.Value.Message.Should().Be("Challenge not found.");
        notFound.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Owner_And_Not_Admin()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "group1",
            "creator",
            "Name",
            "Desc",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1), 
            [], 
            "unit",
            10
        );
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var result = await DeleteChallenge.Handle(
            "group1",
            "ch1",
            dbContext,
            CreateClaimsPrincipal("user2"),
            CreateHttpContext("user2"),
            NullLogger<DeleteChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
        (await dbContext.Challenges.CountAsync()).Should().Be(1);
    }
}