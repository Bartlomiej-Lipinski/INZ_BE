using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Quizzes;
using Mates.Features.Quizzes.Dtos;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Quizzes;

public class GetGroupQuizzesTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_All_Quizzes_For_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");

        var quiz1 = TestDataFactory.CreateQuiz("q1", group.Id, user.Id, "Quiz 1");
        var quiz2 = TestDataFactory.CreateQuiz("q2", group.Id, user.Id, "Quiz 2");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.Quizzes.AddRange(quiz1, quiz2);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupQuizzes.Handle(
            group.Id,
            dbContext,
            CreateHttpContext(user.Id),
            NullLogger<GetGroupQuizzes>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<QuizResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<QuizResponseDto>>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().HaveCount(2);
        ok.Value.Data.Select(q => q.Title).Should().Contain(["Quiz 1", "Quiz 2"]);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_List_When_No_Quizzes()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupQuizzes.Handle(
            group.Id,
            dbContext,
            CreateHttpContext("u1"),
            NullLogger<GetGroupQuizzes>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<QuizResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<QuizResponseDto>>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().BeEmpty();
        ok.Value.Message.Should().Be("No quizzes found for this group.");
    }
}