using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Quizzes;
using WebApplication1.Features.Quizzes.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Quizzes;

public class GetQuizByIdTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Quiz_Without_Correct_Answers()
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

        var result = await GetQuizById.Handle(
            group.Id,
            quiz.Id,
            dbContext,
            CreateHttpContext(user.Id),
            NullLogger<GetQuizById>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<QuizResponseDto>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<QuizResponseDto>>;
        ok!.Value!.Success.Should().BeTrue();

        var dto = ok.Value.Data;
        dto?.Id.Should().Be(quiz.Id);
        dto?.Questions.Should().HaveCount(2);
        dto?.Questions.All(q => q.CorrectTrueFalse == null).Should().BeTrue();
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

        var result = await GetQuizById.Handle(
            group.Id,
            "nonexistentQuizId",
            dbContext,
            CreateHttpContext(user.Id),
            NullLogger<GetQuizById>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Quiz_With_Questions_And_Options()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        var quiz = TestDataFactory.CreateQuiz("q2", group.Id, user.Id, "Quiz Test");

        var question = TestDataFactory.CreateSingleChoiceQuestion(
            "q2_q1",
            quiz.Id,
            "Select one",
            [
                ("Option A", false),
                ("Option B", true)
            ]
        ); 
        
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Quizzes.Add(quiz);
        dbContext.QuizQuestions.Add(question);
        await dbContext.SaveChangesAsync();

        var result = await GetQuizById.Handle(
            group.Id,
            quiz.Id,
            dbContext,
            CreateHttpContext(user.Id),
            NullLogger<GetQuizById>.Instance,
            CancellationToken.None
        );

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<QuizResponseDto>>;
        ok.Should().NotBeNull();

        var dto = ok.Value!.Data;
        dto?.Title.Should().Be("Quiz Test");
        dto?.Questions.Should().HaveCount(1);

        var q = dto?.Questions.First();
        q?.Options.Should().HaveCount(2);
    }
}