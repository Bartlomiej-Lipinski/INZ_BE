using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Quizzes.Attempts;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Quizzes.Attempts;

public class PostQuizAttemptTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Save_Attempt_And_Return_Correct_Score()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, user.Id, "Math Quiz");

        var q1 = TestDataFactory.CreateSingleChoiceQuestion(
            "q1_q1",
            quiz.Id,
            "2 + 2?",
            [("4", true), ("5", false)]
        );

        var q2 = TestDataFactory.CreateTrueFalseQuestion(
            "q1_q2",
            quiz.Id,
            "Sky is blue?",
            true
        );

        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Quizzes.Add(quiz);
        dbContext.QuizQuestions.AddRange(q1, q2);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateQuizAttemptRequestDto(
            [
                TestDataFactory.CreateQuizAttemptAnswerRequestDto(
                    q1.Id, selectedOptionId: q1.Options.First(o => o.IsCorrect).Id),
                TestDataFactory.CreateQuizAttemptAnswerRequestDto(q2.Id, selectedTrueFalse: true)
            ]
        );

        var result = await PostQuizAttempt.Handle(
            group.Id,
            quiz.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostQuizAttempt>.Instance,
            CancellationToken.None
        );

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<int>>;
        ok.Should().NotBeNull();

        ok.Value!.Data.Should().Be(2);

        var attempt = await dbContext.QuizAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.QuizId == quiz.Id);

        attempt.Should().NotBeNull();
        attempt.Score.Should().Be(2);
        attempt.Answers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Quiz_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateQuizAttemptRequestDto([]);

        var result = await PostQuizAttempt.Handle(
            group.Id,
            "missingQuiz",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostQuizAttempt>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Answers_Count_Is_Invalid()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, user.Id, "Quiz!");

        var q1 = TestDataFactory.CreateTrueFalseQuestion(
            "q1_q1",
            quiz.Id,
            "Statement?",
            true
        );

        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Quizzes.Add(quiz);
        dbContext.QuizQuestions.Add(q1);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateQuizAttemptRequestDto([]);

        var result = await PostQuizAttempt.Handle(
            group.Id,
            quiz.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostQuizAttempt>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }
}