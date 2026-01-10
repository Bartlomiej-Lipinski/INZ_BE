using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Challenges.Progress;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Challenges.Progress;

public class DeleteChallengeProgressTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Delete_Progress()
    {
        var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1", 
            "g1",
            "u1",
            "Run", 
            "Run 5km",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(5), 
            [], 
            "km",
            5
        );

        var participant = TestDataFactory.CreateChallengeParticipant("ch1", "u1", DateTime.UtcNow, 2);
        var progress = TestDataFactory.CreateChallengeProgress("p1", "ch1", "u1", "Ran 2km", 2);

        db.Challenges.Add(challenge);
        db.ChallengeParticipants.Add(participant);
        db.ChallengeProgresses.Add(progress);
        await db.SaveChangesAsync();

        var result = await DeleteChallengeProgress.Handle(
            "g1", 
            "ch1",
            "p1",
            db,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<DeleteChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await db.ChallengeProgresses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Challenge_Does_Not_Exist()
    {
        var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await DeleteChallengeProgress.Handle(
            "g1",
            "missingChallenge", 
            "p1", 
            db,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<DeleteChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Participant_Does_Not_Exist()
    {
        var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "g1", 
            "u1",
            "Run", 
            "Run 5km", 
            DateTime.UtcNow, 
            DateTime.UtcNow.AddDays(5),
            [], 
            "km",
            5
        );

        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var result = await DeleteChallengeProgress.Handle(
            "g1",
            "ch1", 
            "p1",
            db,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<DeleteChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Progress_Does_Not_Exist()
    {
        var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1", 
            "g1",
            "u1",
            "Run",
            "Run 5km",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(5), 
            [], 
            "km",
            5
        );

        var participant = TestDataFactory.CreateChallengeParticipant("ch1", "u1", DateTime.UtcNow, 2);

        db.Challenges.Add(challenge);
        db.ChallengeParticipants.Add(participant);
        await db.SaveChangesAsync();

        var result = await DeleteChallengeProgress.Handle(
            "g1",
            "ch1",
            "missingProgress", 
            db,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<DeleteChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}