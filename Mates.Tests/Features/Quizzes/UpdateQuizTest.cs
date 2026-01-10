using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Quizzes;
using Mates.Infrastructure.Data.Entities.Quizzes;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Quizzes;

public class UpdateQuizTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Update_Quiz_When_User_Is_Owner()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, user.Id, "Old Title");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateQuizRequestDto(
            "New Title",
            [
                TestDataFactory.CreateSingleChoiceQuestion("Question 1?", new[]
                {
                    ("Option 1", true),
                    ("Option 2", false)
                }.ToList()),

                TestDataFactory.CreateTrueFalseQuestion("Question 2?", true)
            ]
        );
        
        var httpContext = CreateHttpContext(user.Id);
        httpContext.Items["GroupUser"] = groupUser;

        var result = await UpdateQuiz.Handle(
            group.Id,
            quiz.Id,
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            httpContext,
            NullLogger<UpdateQuiz>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();

        var updatedQuiz = await dbContext.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(qt => qt.Options)
            .FirstOrDefaultAsync(q => q.Id == quiz.Id);

        updatedQuiz!.Title.Should().Be("New Title");
        updatedQuiz.Questions.Should().HaveCount(2);
        updatedQuiz.Questions.First(q => q.Type == QuizQuestionType.SingleChoice).Options.Should().HaveCount(2);
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

        var request = TestDataFactory.CreateQuizRequestDto("New Quiz", []);

        var httpContext = CreateHttpContext(user.Id);
        httpContext.Items["GroupUser"] = groupUser;
        
        var result = await UpdateQuiz.Handle(
            group.Id,
            "nonexistent",
            request,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateQuiz>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Owner_Or_Admin()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, "u1", "Old Title");
        dbContext.Groups.Add(group);
        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateQuizRequestDto(
            "New Title",
            [
                TestDataFactory.CreateSingleChoiceQuestion("Question 1?", new[]
                {
                    ("Option 1", true),
                    ("Option 2", false)
                }.ToList())
            ]
        );

        var result = await UpdateQuiz.Handle(
            group.Id,
            quiz.Id,
            request,
            dbContext,
            CreateClaimsPrincipal("otherUser"),
            CreateHttpContext("otherUser"),
            NullLogger<UpdateQuiz>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
}