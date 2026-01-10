using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Quizzes.Attempts;
using Mates.Features.Quizzes.Dtos;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Quizzes.Attempts;

public class GetUsersLastQuizAttemptTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Last_Attempt()
    {
        var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Group");
        var user = TestDataFactory.CreateUser("u1", "A", "B");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, user.Id, "Quiz");

        db.Groups.Add(group);
        db.Users.Add(user);
        db.GroupUsers.Add(groupUser);
        db.Quizzes.Add(quiz);

        var attempt1 = TestDataFactory.CreateQuizAttempt("a1", quiz.Id, user.Id, 1, DateTime.UtcNow.AddMinutes(-10));
        var attempt2 = TestDataFactory.CreateQuizAttempt("a2", quiz.Id, user.Id, 2, DateTime.UtcNow);

        db.QuizAttempts.AddRange(attempt1, attempt2);
        await db.SaveChangesAsync();

        var result = await GetUsersLastQuizAttempt.Handle(
            group.Id,
            quiz.Id,
            db,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetUsersLastQuizAttempt>.Instance,
            CancellationToken.None
        );

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<QuizAttemptResponseDto>>;
        ok.Should().NotBeNull();

        ok.Value!.Data?.AttemptId.Should().Be("a2");
        ok.Value.Data?.Score.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_User_Has_No_Attempts()
    {
        var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Group");
        var user = TestDataFactory.CreateUser("u1", "A", "B");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, user.Id, "Quiz");
        db.Groups.Add(group);
        db.Users.Add(user);
        db.GroupUsers.Add(groupUser);
        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();

        var result = await GetUsersLastQuizAttempt.Handle(
            group.Id,
            quiz.Id,
            db,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetUsersLastQuizAttempt>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}