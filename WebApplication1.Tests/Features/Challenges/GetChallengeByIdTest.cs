using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Challenges;
using WebApplication1.Features.Challenges.Dtos;
using WebApplication1.Infrastructure.Data.Entities.Challenges;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Challenges;

public class GetChallengeByIdTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Challenge_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await GetChallengeById.Handle(
            "nonexistent-group",
            "nonexistent-challenge",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetChallengeById>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value!.Success.Should().BeFalse();
        notFound.Value.Message.Should().Be("Challenge not found.");
    }

    [Fact]
    public async Task Handle_Should_Return_Challenge_With_Participants_And_Stages()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);

        var challenge = TestDataFactory.CreateChallenge(
            "c1",
            group.Id,
            user.Id,
            "Challenge 1",
            "Test Challenge",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(5),
            [
                new ChallengeParticipant
                {
                    ChallengeId = "c1",
                    UserId = user.Id,
                    ProgressEntries = new List<ChallengeProgress>
                    {
                        new()
                        {
                            Id = "p1",
                            Date = DateTime.UtcNow,
                            Description = "Initial progress",
                            Value = 3
                        }
                    }
                }
            ],
            "km",
            10
        );

        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var result = await GetChallengeById.Handle(
            group.Id,
            challenge.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetChallengeById>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<ChallengeResponseDto>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<ChallengeResponseDto>>;
        okResult!.Value!.Success.Should().BeTrue();

        var data = okResult.Value.Data!;
        data.Id.Should().Be(challenge.Id);
        data.Name.Should().Be("Challenge 1");
        data.Participants.Should().HaveCount(1);
        data.Participants.First().UserId.Should().Be(user.Id);
        data.Participants.First().ProgressEntries.Should().HaveCount(1);
    }
}