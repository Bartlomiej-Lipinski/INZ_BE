using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Challenges;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Challenges;

public class UpdateChallengeTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Update_Challenge_Successfully()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("user1", "Test", "User");
        dbContext.Users.Add(user);

        var challenge = TestDataFactory.CreateChallenge(
            "ch1", 
            "group1", 
            user.Id, 
            "Old Name",
            "Old Description", 
            DateTime.UtcNow, 
            DateTime.UtcNow.AddDays(1), 
            [], 
            "km",
            10
        );

        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeRequestDto(
            "New Name",
            "New Description",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            "steps",
            4000
        );

        var result = await UpdateChallenge.Handle(
            "group1",
            "ch1",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value!.Data.Should().Be("Challenge updated successfully.");
        ok.Value.TraceId.Should().Be("test-trace-id");

        var updated = await dbContext.Challenges.FindAsync("ch1");
        updated!.Name.Should().Be("New Name");
        updated.Description.Should().Be("New Description");
        updated.GoalUnit.Should().Be("steps");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Challenge_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var request = TestDataFactory.CreateChallengeRequestDto(
            "Test",
            "Desc",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(2),
            "steps",
            4000
        );

        var result = await UpdateChallenge.Handle(
            "group1",
            "not-found-id",
            request,
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<UpdateChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value!.Success.Should().BeFalse();
        notFound.Value.Message.Should().Be("Challenge not found.");
        notFound.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Creator()
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
            "steps",
            4000
        );
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeRequestDto(
            "New",
            "New Desc",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(2),
            "steps",
            4000
        );

        var result = await UpdateChallenge.Handle(
            "group1",
            "ch1",
            request,
            dbContext,
            CreateClaimsPrincipal("otherUser"),
            CreateHttpContext("otherUser"),
            NullLogger<UpdateChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Name_Or_Description_Missing()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "group1",
            "user1",
            "Old",
            "Old",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1), 
            [], 
            "steps",
            4000
        );
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeRequestDto(
            "", 
            "",
            DateTime.UtcNow, 
            DateTime.UtcNow.AddDays(2),
            "steps",
            4000);

        var result = await UpdateChallenge.Handle(
            "group1",
            "ch1",
            request,
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<UpdateChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var bad = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        bad!.Value!.Success.Should().BeFalse();
        bad.Value.Message.Should().Be("Challenge name and description are required.");
        bad.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_EndDate_Before_StartDate()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var challenge = TestDataFactory.CreateChallenge(
            "ch1",
            "group1",
            "user1",
            "Old",
            "Old",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1), 
            [], 
            "steps",
            4000
        );
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateChallengeRequestDto(
            "New",
            "New",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1),
            "steps",
            4000
        );

        var result = await UpdateChallenge.Handle(
            "group1",
            "ch1",
            request,
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<UpdateChallenge>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var bad = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        bad!.Value!.Success.Should().BeFalse();
        bad.Value.Message.Should().Be("Range end cannot be earlier than range start.");
        bad.Value.TraceId.Should().Be("test-trace-id");
    }
}