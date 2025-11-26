using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Quizzes;
using WebApplication1.Features.Quizzes.Dtos;
using WebApplication1.Infrastructure.Data.Entities.Quizzes;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Quizzes;

public class PostQuizTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Create_Quiz_When_Valid()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateQuizRequestDto("Sample Quiz", new List<QuizQuestionRequestDto>
        {
            TestDataFactory.CreateSingleChoiceQuestion("What is 2+2?", new List<(string, bool)>
            {
                ("3", false),
                ("4", true)
            }),
            TestDataFactory.CreateTrueFalseQuestion("The sky is blue.", true)
        });

        var result = await PostQuiz.Handle(
            group.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostQuiz>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().NotBeNull();

        var quizInDb = await dbContext.Quizzes.Include(q => q.Questions).ThenInclude(qt => qt.Options).FirstOrDefaultAsync();
        quizInDb.Should().NotBeNull();
        quizInDb.Title.Should().Be("Sample Quiz");
        quizInDb.Questions.Should().HaveCount(2);
        quizInDb.Questions.First(q => q.Type == QuizQuestionType.SingleChoice).Options.Should().HaveCount(2);
        quizInDb.Questions.First(q => q.Type == QuizQuestionType.SingleChoice).Options.Count(o => o.IsCorrect).Should().Be(1);
        quizInDb.Questions.First(q => q.Type == QuizQuestionType.TrueFalse).CorrectTrueFalse.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Title_Missing()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var request = TestDataFactory.CreateQuizRequestDto("", []);

        var result = await PostQuiz.Handle(
            "g1",
            request,
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<PostQuiz>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Questions_Empty()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var request = TestDataFactory.CreateQuizRequestDto("Quiz", []);

        var result = await PostQuiz.Handle(
            "g1",
            request,
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<PostQuiz>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_SingleChoice_Invalid()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var request = TestDataFactory.CreateQuizRequestDto("Quiz", [
            TestDataFactory.CreateSingleChoiceQuestion("Q1", [
                ("A", true)
            ])
        ]);

        var result = await PostQuiz.Handle(
            "g1",
            request,
            dbContext,
            CreateClaimsPrincipal("u1"),
            CreateHttpContext("u1"),
            NullLogger<PostQuiz>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }
}