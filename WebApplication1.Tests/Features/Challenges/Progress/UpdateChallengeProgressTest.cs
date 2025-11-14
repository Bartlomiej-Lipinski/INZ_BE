using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Challenges.Progress;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Challenges.Progress;

public class UpdateChallengeProgressTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Update_Progress_And_TotalProgress()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var challenge = TestDataFactory.CreateChallenge(
            "ch1", 
            "g1", 
            "u1", 
            "Steps",
            "Walk 10000 steps",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(10), 
            [], 
            "steps", 
            10000
        );
        dbContext.Challenges.Add(challenge);

        var participant = TestDataFactory.CreateChallengeParticipant(
            challenge.Id, "u1", DateTime.UtcNow, 1000);
        dbContext.ChallengeParticipants.Add(participant);

        var progress = TestDataFactory.CreateChallengeProgress(
            "p1",
            challenge.Id,
            "u1",
            "Walked 1000 steps",
            1000
        );

        dbContext.ChallengeProgresses.Add(progress);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeProgressRequestDto("Walked 1500 steps", 1500);

        var result = await UpdateChallengeProgress.Handle(
            "g1",
            "ch1",
            "p1",
            request,
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<UpdateChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var updatedProgress = await dbContext.ChallengeProgresses.FirstAsync();
        updatedProgress.Description.Should().Be("Walked 1500 steps");
        updatedProgress.Value.Should().Be(1500);

        var updatedParticipant = await dbContext.ChallengeParticipants.FirstAsync();
        updatedParticipant.TotalProgress.Should().Be(1500);
    }

    [Fact]
    public async Task Handle_Should_Mark_Participant_As_Completed_When_Goal_Reached()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "g1",
            "u1", 
            "Pushups", 
            "Do 50 pushups",
            DateTime.UtcNow, 
            DateTime.UtcNow.AddDays(7),
            [], 
            "reps",
            50
        );
        dbContext.Challenges.Add(challenge);

        var participant = TestDataFactory.CreateChallengeParticipant(
            challenge.Id, "u1", DateTime.UtcNow, 45);
        dbContext.ChallengeParticipants.Add(participant);

        var progress = TestDataFactory.CreateChallengeProgress(
            "p1",
            challenge.Id,
            "u1",
            "Did 45 pushups",
            45
        );
        dbContext.ChallengeProgresses.Add(progress);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeProgressRequestDto("Added final 50 pushups", 50);

        var result = await UpdateChallengeProgress.Handle(
            "g1",
            "ch1",
            "p1",
            request,
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<UpdateChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var updatedParticipant = await dbContext.ChallengeParticipants.FirstAsync();
        updatedParticipant.Completed.Should().BeTrue();
        updatedParticipant.TotalProgress.Should().Be(50);
        updatedParticipant.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_Not_Found_If_User_Does_Not_Own_Progress()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1", 
            "g1",
            "owner1",
            "Run", 
            "Run 5km", 
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(10), 
            [],
            "km",
            5
        );
        dbContext.Challenges.Add(challenge);

        var participant = TestDataFactory.CreateChallengeParticipant(challenge.Id, "user1", DateTime.UtcNow, 2);
        dbContext.ChallengeParticipants.Add(participant);

        var progress = TestDataFactory.CreateChallengeProgress(
            "p1",
            challenge.Id,
            "user1",
            "Ran 2km",
            2
        );
        dbContext.ChallengeProgresses.Add(progress);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeProgressRequestDto("Ran 3km", 3);

        var result = await UpdateChallengeProgress.Handle(
            "g1",
            "ch1",
            "p1",
            request,
            dbContext,
            CreateClaimsPrincipal("otherUser"),
            CreateHttpContext("otherUser"),
            NullLogger<UpdateChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_If_Value_Is_Invalid()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1", 
            "g1", 
            "u1", 
            "Situps", 
            "Do 100 situps",
            DateTime.UtcNow, 
            DateTime.UtcNow.AddDays(5), 
            [],
            "reps", 
            100
        );
        dbContext.Challenges.Add(challenge);

        var participant = TestDataFactory.CreateChallengeParticipant(challenge.Id, "u1", DateTime.UtcNow, 10);
        dbContext.ChallengeParticipants.Add(participant);

        var progress = TestDataFactory.CreateChallengeProgress(
            "p1",
            challenge.Id,
            "u1",
            "Did 10 situps",
            10
        );
        dbContext.ChallengeProgresses.Add(progress);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeProgressRequestDto("", -5);

        var result = await UpdateChallengeProgress.Handle(
            "g1",
            "ch1",
            "p1",
            request,
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<UpdateChallengeProgress>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }
}