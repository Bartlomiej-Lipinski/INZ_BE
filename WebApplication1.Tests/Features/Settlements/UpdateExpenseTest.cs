using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebApplication1.Features.Settlements;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Settlements;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Settlements;

public class UpdateExpenseTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var dto = TestDataFactory.CreateExpenseRequestDto("Test Expense", user.Id, 100, true,
            [new ExpenseBeneficiaryDto { UserId = "u1" }]);
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), false,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await UpdateExpense.Handle(
            "nonexistent-group",
            "e1",
            dto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Expense_Not_Found()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var dto = TestDataFactory.CreateExpenseRequestDto("Test Expense", user.Id, 100, true,
            [new ExpenseBeneficiaryDto { UserId = user.Id }]);
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), false,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await UpdateExpense.Handle(
            group.Id,
            "nonexistent-expense",
            dto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Update_Expense_When_Valid_Request()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var expense = TestDataFactory.CreateExpense("e1", group.Id, user.Id, "Old Title", 100, true);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync();

        var dto = TestDataFactory.CreateExpenseRequestDto("Updated Expense", user.Id, 120, true,
            [new ExpenseBeneficiaryDto { UserId = user.Id }]);
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), false,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await UpdateExpense.Handle(
            group.Id,
            expense.Id,
            dto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<UpdateExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();

        var updatedExpense = await dbContext.Expenses.Include(e => e.Beneficiaries).FirstAsync();
        updatedExpense.Title.Should().Be("Updated Expense");
        updatedExpense.Amount.Should().Be(120);
        updatedExpense.Beneficiaries.Should().HaveCount(1);
        updatedExpense.Beneficiaries.First().Share.Should().Be(120);
    }
}