using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Challenges.Participants;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Challenges.Participants;

public class DeleteChallengeParticipantTest : TestBase
{
    [Fact]
    public async Task Should_Delete_When_Participant_Deletes_Themself()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "g1",
            "owner",
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
            "ch1",
            "user1",
            DateTime.UtcNow,
            0
        );
        dbContext.ChallengeParticipants.Add(participant);
        await dbContext.SaveChangesAsync();

        var result = await DeleteChallengeParticipant.Handle(
            "g1",
            "ch1",
            "user1",
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<DeleteChallengeParticipant>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.ChallengeParticipants.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Should_Delete_When_Owner_Deletes_Participant()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "g1",
            "owner",
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
            "ch1",
            "user1",
            DateTime.UtcNow,
            0
        );
        dbContext.ChallengeParticipants.Add(participant);
        await dbContext.SaveChangesAsync();

        var result = await DeleteChallengeParticipant.Handle(
            "g1",
            "ch1",
            "user1",
            dbContext,
            CreateClaimsPrincipal("owner"),
            CreateHttpContext("owner"),
            NullLogger<DeleteChallengeParticipant>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        (await dbContext.ChallengeParticipants.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_User_Is_Not_Owner_Or_Participant()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "g1",
            "owner",
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
            "ch1",
            "user1",
            DateTime.UtcNow,
            0
        );
        dbContext.ChallengeParticipants.Add(participant);
        await dbContext.SaveChangesAsync();

        var result = await DeleteChallengeParticipant.Handle(
            "g1",
            "ch1",
            "user1",
            dbContext,
            CreateClaimsPrincipal("otherUser"),
            CreateHttpContext("otherUser"),
            NullLogger<DeleteChallengeParticipant>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
        (await dbContext.ChallengeParticipants.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Challenge_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await DeleteChallengeParticipant.Handle(
            "g1",
            "nonexistent",
            "user1",
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<DeleteChallengeParticipant>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Participant_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "g1",
            "owner",
            "Test Challenge",
            "Some description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14),
            [],
            "km",
            20
        );
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var result = await DeleteChallengeParticipant.Handle(
            "g1",
            "ch1",
            "nonexistentParticipant",
            dbContext,
            CreateClaimsPrincipal("owner"),
            CreateHttpContext("owner"),
            NullLogger<DeleteChallengeParticipant>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}