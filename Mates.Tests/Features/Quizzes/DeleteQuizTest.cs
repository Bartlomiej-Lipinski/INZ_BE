using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Quizzes;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Quizzes;

public class DeleteQuizTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Delete_Quiz_When_User_Is_Owner()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Group 1");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id, isAdmin: false);

        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, user.Id, "Test");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();
        
        var httpContext = CreateHttpContext(user.Id);
        httpContext.Items["GroupUser"] = groupUser;
        
        var result = await DeleteQuiz.Handle(
            group.Id,
            quiz.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteQuiz>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        (result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>)!.Value!.Success.Should().BeTrue();

        var deletedQuiz = await dbContext.Quizzes.FirstOrDefaultAsync();
        deletedQuiz.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Delete_Quiz_When_User_Is_Admin()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Group 1");
        var owner = TestDataFactory.CreateUser("u1", "Quiz", "Owner");
        var admin = TestDataFactory.CreateUser("u2", "Admin", "User");

        var groupOwner = TestDataFactory.CreateGroupUser(owner.Id, group.Id);
        var groupAdmin = TestDataFactory.CreateGroupUser(admin.Id, group.Id, true);
        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, owner.Id, "Test");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(owner);
        dbContext.Users.Add(admin);
        dbContext.GroupUsers.Add(groupOwner);
        dbContext.GroupUsers.Add(groupAdmin);
        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();
        
        var httpContext = CreateHttpContext(admin.Id);
        httpContext.Items["GroupUser"] = groupAdmin;
        
        var result = await DeleteQuiz.Handle(
            group.Id,
            quiz.Id,
            dbContext,
            CreateClaimsPrincipal(admin.Id),
            httpContext,
            NullLogger<DeleteQuiz>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        var deletedQuiz = await dbContext.Quizzes.FirstOrDefaultAsync();
        deletedQuiz.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Quiz_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Group 1");
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var result = await DeleteQuiz.Handle(
            group.Id,
            "nonexistent",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteQuiz>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Is_Not_Owner_Or_Admin()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Group 1");
        var owner = TestDataFactory.CreateUser("u1", "Owner", "User");
        var otherUser = TestDataFactory.CreateUser("u2", "Random", "User");
        var groupOwner = TestDataFactory.CreateGroupUser(owner.Id, group.Id);
        var groupOther = TestDataFactory.CreateGroupUser(otherUser.Id, group.Id);

        var quiz = TestDataFactory.CreateQuiz("q1", group.Id, owner.Id, "Test");
        dbContext.Groups.Add(group);
        dbContext.Users.Add(owner);
        dbContext.Users.Add(otherUser);
        dbContext.GroupUsers.Add(groupOwner);
        dbContext.GroupUsers.Add(groupOther);
        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync();
        
        var result = await DeleteQuiz.Handle(
            group.Id,
            quiz.Id,
            dbContext,
            CreateClaimsPrincipal(otherUser.Id),
            CreateHttpContext(otherUser.Id),
            NullLogger<DeleteQuiz>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
}