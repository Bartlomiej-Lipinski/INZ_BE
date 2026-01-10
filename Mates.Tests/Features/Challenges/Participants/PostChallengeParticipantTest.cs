using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Challenges.Participants;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Challenges.Participants;

public class PostChallengeParticipantTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Challenge_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await PostChallengeParticipant.Handle(
            "group1",
            "missing-challenge",
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<PostChallengeParticipant>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value!.Success.Should().BeFalse();
        notFound.Value.Message.Should().Be("Challenge not found.");
        notFound.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_User_Already_Participates()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "challenge1",
            "group1",
            "owner1",
            "Test Challenge",
            "Some description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14),
            [],
            "km",
            20
        );
        dbContext.Challenges.Add(challenge);

        var participant = TestDataFactory.CreateChallengeParticipant(
            "challenge1",
            "user1",
            DateTime.UtcNow,
            0
        );
        dbContext.ChallengeParticipants.Add(participant);

        await dbContext.SaveChangesAsync();

        var result = await PostChallengeParticipant.Handle(
            "group1",
            "challenge1",
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<PostChallengeParticipant>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value!.Success.Should().BeFalse();
        badRequest.Value.Message.Should().Be("User already joined this challenge.");
        badRequest.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Add_Participant_Successfully()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "challenge1",
            "group1",
            "owner1",
            "Challenge Name",
            "Description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14),
            [],
            "km",
            20
        );
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var result = await PostChallengeParticipant.Handle(
            "group1",
            "challenge1",
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<PostChallengeParticipant>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().Be("Challenge joined successfully.");
        ok.Value.TraceId.Should().Be("test-trace-id");

        var participant = await dbContext.ChallengeParticipants.FirstOrDefaultAsync(p => p.UserId == "user1");
        participant.Should().NotBeNull();
        participant.ChallengeId.Should().Be("challenge1");
        participant.TotalProgress.Should().Be(0);
        participant.Completed.Should().BeFalse();
    }
}