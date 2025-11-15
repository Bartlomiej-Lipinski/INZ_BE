using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
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

public class PostExpenseTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Create_Expense_When_User_Is_Group_Member_And_EvenSplit()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Trip Group");
        var user1 = TestDataFactory.CreateUser("u1", "Test","User");
        var user2 = TestDataFactory.CreateUser("u2", "Test","User");
        var gu1 = TestDataFactory.CreateGroupUser(user1.Id, group.Id);
        var gu2 = TestDataFactory.CreateGroupUser(user2.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.AddRange(user1, user2);
        dbContext.GroupUsers.AddRange(gu1, gu2);
        await dbContext.SaveChangesAsync();
        
        var dto = TestDataFactory.CreateExpenseRequestDto("Dinner", user1.Id, 100, true, [
            new ExpenseBeneficiaryDto { UserId = user1.Id },
            new ExpenseBeneficiaryDto { UserId = user2.Id }
        ]);
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), true,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var result = await PostExpense.Handle(
            group.Id,
            dto,
            dbContext,
            CreateClaimsPrincipal(user1.Id),
            CreateHttpContext(user1.Id),
            NullLogger<PostExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None);

        result.Should().BeOfType<Ok<ApiResponse<string>>>();
        var ok = result as Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().Be("Expense created successfully.");

        var expense = await dbContext.Expenses
            .Include(e => e.Beneficiaries)
            .FirstOrDefaultAsync();

        expense.Should().NotBeNull();
        expense.Title.Should().Be("Dinner");
        expense.Amount.Should().Be(100);
        expense.Beneficiaries.Should().HaveCount(2);
        expense.Beneficiaries.Select(b => b.Share).Should().AllBeEquivalentTo(50m);
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Missing_Required_Fields()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Group");
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var dto = TestDataFactory.CreateExpenseRequestDto(
            "",
            "", 
            10,
            true, 
            []);

        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), true,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var result = await PostExpense.Handle(
            group.Id,
            dto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Shares_Do_Not_Sum_To_Amount()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var group = TestDataFactory.CreateGroup("g1", "Group");
        var user = TestDataFactory.CreateUser("u1", "Test","User");
        var gu = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        dbContext.Groups.Add(group);
        dbContext.Users.Add(user);
        dbContext.GroupUsers.Add(gu);
        await dbContext.SaveChangesAsync();

        var dto = TestDataFactory.CreateExpenseRequestDto(
            "Trip",
            user.Id, 
            100,
            false,
            [
                new ExpenseBeneficiaryDto { UserId = user.Id, Share = 30 },
                new ExpenseBeneficiaryDto { UserId = "u2", Share = 60 }
            ]);
        
        var mockCalculator = new Mock<ISettlementCalculator>();
        mockCalculator
            .Setup(c => c.RecalculateSettlementsForExpenseChangeAsync(
                It.IsAny<Expense>(), It.IsAny<AppDbContext>(), It.IsAny<string>(), true,
                It.IsAny<ILogger>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await PostExpense.Handle(
            group.Id,
            dto,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostExpense>.Instance,
            mockCalculator.Object,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
    }
}