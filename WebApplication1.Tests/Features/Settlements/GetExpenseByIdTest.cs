using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Settlements;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Settlements;

public class GetExpenseByIdTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var result = await GetExpenseById.Handle(
            "nonexistent-group",
            "expense1",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostExpense>.Instance,
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

        var result = await GetExpenseById.Handle(
            group.Id,
            "nonexistent-expense",
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostExpense>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Expense_When_Exists_And_User_Is_Member()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);
        var expense = TestDataFactory.CreateExpense("e1", group.Id, user.Id, "Test Expense",100m, true);

        var beneficiary = TestDataFactory.CreateExpenseBeneficiary(expense.Id, user.Id, 100m);
        expense.Beneficiaries.Add(beneficiary);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.Expenses.Add(expense);
        dbContext.ExpenseBeneficiaries.Add(beneficiary);
        await dbContext.SaveChangesAsync();

        var result = await GetExpenseById.Handle(
            group.Id,
            expense.Id,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<PostExpense>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<ExpenseResponseDto>>>();

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<ExpenseResponseDto>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Data.Should().NotBeNull();
        ok.Value.Data!.Id.Should().Be(expense.Id);
        ok.Value.Data!.Title.Should().Be("Test Expense");
        ok.Value.Data!.GroupId.Should().Be(group.Id);
        ok.Value.Data!.PaidByUserId.Should().Be(user.Id);
        ok.Value.Data!.Beneficiaries.Should().HaveCount(1);
        ok.Value.Data!.Beneficiaries.First().Share.Should().Be(100m);
    }
}