using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Challenges.Progress;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Challenges.Progress;

public class PostChallengeProgressTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Challenge_Not_Found()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var request = TestDataFactory.CreateChallengeProgressRequestDto("Ran 5km", 5);

        var result = await PostChallengeProgress.Handle(
            "group1",
            "missing-challenge",
            request,
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<PostChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value!.Success.Should().BeFalse();
        notFound.Value.Message.Should().Be("Challenge not found.");
        notFound.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Participant_Not_Found()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var challenge = TestDataFactory.CreateChallenge(
            "challenge1",
            "group1",
            "owner1",
            "Challenge",
            "Desc",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14),
            [],
            "km",
            20
        );
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeProgressRequestDto("Did 10 pushups", 10);

        var result = await PostChallengeProgress.Handle(
            "group1",
            "challenge1",
            request,
            dbContext,
            CreateClaimsPrincipal("user2"),
            CreateHttpContext("user2"),
            NullLogger<PostChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value!.Success.Should().BeFalse();
        notFound.Value.Message.Should().Be("Challenge participant not found.");
        notFound.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Add_Progress_And_Update_Participant()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "challenge1",
            "group1",
            "owner1",
            "Steps",
            "Walk 10,000 steps",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14),
            [],
            "km",
            100
        );
        dbContext.Challenges.Add(challenge);

        var participant = TestDataFactory.CreateChallengeParticipant(
            "challenge1",
            "user1",
            DateTime.UtcNow,
            20
        );

        dbContext.ChallengeParticipants.Add(participant);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeProgressRequestDto("Walked 10 more steps", 10);

        var result = await PostChallengeProgress.Handle(
            "group1",
            "challenge1",
            request,
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<PostChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().Be("Challenge joined successfully.");
        ok.Value.TraceId.Should().Be("test-trace-id");

        var updatedParticipant = await dbContext.ChallengeParticipants.FirstAsync();
        updatedParticipant.TotalProgress.Should().Be(30);
        updatedParticipant.Completed.Should().BeFalse();

        var progress = await dbContext.ChallengeProgresses.FirstAsync();
        progress.Description.Should().Be("Walked 10 more steps");
        progress.Value.Should().Be(10);
    }

    [Fact]
    public async Task Handle_Should_Mark_Challenge_And_Participant_As_Completed_When_Goal_Reached()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "challenge1",
            "group1",
            "owner1",
            "Pushups",
            "Do 50 pushups",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14),
            [],
            "pushups",
            50
        );
        dbContext.Challenges.Add(challenge);

        var participant = TestDataFactory.CreateChallengeParticipant(
            "challenge1",
            "user1",
            DateTime.UtcNow,
            45
        );
        dbContext.ChallengeParticipants.Add(participant);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeProgressRequestDto("Did final 5 pushups", 5);

        var result = await PostChallengeProgress.Handle(
            "group1",
            "challenge1",
            request,
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<PostChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();

        var updatedParticipant = await dbContext.ChallengeParticipants.FirstAsync();
        updatedParticipant.Completed.Should().BeTrue();
        updatedParticipant.TotalProgress.Should().Be(50);
        updatedParticipant.CompletedAt.Should().NotBeNull();

        var updatedChallenge = await dbContext.Challenges.FirstAsync();
        updatedChallenge.IsCompleted.Should().BeTrue();
    }
}