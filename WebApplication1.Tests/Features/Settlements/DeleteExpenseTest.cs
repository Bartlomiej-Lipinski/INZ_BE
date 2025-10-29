using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebApplication1.Features.Settlements;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Settlements;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Settlements;

public class DeleteExpenseTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), false,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await DeleteExpense.Handle(
            "g1",
            "e1",
            dbContext,
            CreateClaimsPrincipal(),
            CreateHttpContext(),
            NullLogger<DeleteExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), false,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await DeleteExpense.Handle(
            "nonexistent-group",
            "e1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbid_When_User_Not_Member_Of_Group()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), false,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await DeleteExpense.Handle(
            group.Id,
            "e1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Expense_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), false,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await DeleteExpense.Handle(
            group.Id,
            "nonexistent-expense",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Delete_Expense_And_Recalculate_Settlements()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var expense = TestDataFactory.CreateExpense("e1", group.Id, user.Id, "title", 100m, true);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync();
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), false,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await DeleteExpense.Handle(
            group.Id,
            expense.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<DeleteExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data.Should().Contain("deleted successfully");

        dbContext.Expenses.Count().Should().Be(0);
    }
}