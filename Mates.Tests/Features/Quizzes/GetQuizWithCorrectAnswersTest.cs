using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Quizzes;
using Mates.Features.Quizzes.Dtos;
using Mates.Infrastructure.Data.Entities.Quizzes;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Quizzes;

public class GetQuizWithCorrectAnswersTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Quiz_With_Correct_Answers()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, user.Id, "Sample Quiz");

        var question1 = TestDataFactory.CreateSingleChoiceQuestion(
            "q1_q1",
            quiz.Id,
            "What is 2 + 2?",
            [
                ("4", true),
                ("5", false)
            ]
        );

        var question2 = TestDataFactory.CreateTrueFalseQuestion("q1_q2", quiz.Id, "Sky is blue?", true);

        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Quizzes.Add(quiz);
        dbContext.QuizQuestions.AddRange(question1, question2);
        await dbContext.SaveChangesAsync();

        var result = await GetQuizWithCorrectAnswers.Handle(
            group.Id,
            quiz.Id,
            dbContext,
            CreateHttpContext(user.Id),
            NullLogger<GetQuizWithCorrectAnswers>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<QuizResponseDto>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<QuizResponseDto>>;
        ok!.Value!.Success.Should().BeTrue();

        var dto = ok.Value.Data;
        dto?.Id.Should().Be(quiz.Id);
        dto?.Questions.Should().HaveCount(2);

        var singleChoiceQuestion = dto!.Questions.First(q => q.Type == QuizQuestionType.SingleChoice);
        singleChoiceQuestion.Options.First(o => o.Text == "4").IsCorrect.Should().BeTrue();
        singleChoiceQuestion.Options.First(o => o.Text == "5").IsCorrect.Should().BeFalse();

        var trueFalseQuestion = dto.Questions.First(q => q.Type == QuizQuestionType.TrueFalse);
        trueFalseQuestion.CorrectTrueFalse.Should().BeTrue();
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

        var result = await GetQuizWithCorrectAnswers.Handle(
            group.Id,
            "nonexistentQuizId",
            dbContext,
            CreateHttpContext(user.Id),
            NullLogger<GetQuizWithCorrectAnswers>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }
}