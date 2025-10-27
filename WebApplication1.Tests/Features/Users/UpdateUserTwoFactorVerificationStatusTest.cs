using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Users;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Users;

public class UpdateUserTwoFactorVerificationStatusTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Update_TwoFactorStatus_When_Valid()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var user = TestDataFactory.CreateUser("user1", "Test", "test@test.com", "testUser", "User");
        user.TwoFactorEnabled = false;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        var result = await UpdateUserTwoFactorVerificationStatus.Handle(
            true,
            CreateClaimsPrincipal(user.Id),
            dbContext,
            CreateHttpContext(),
            NullLogger<UpdateUserTwoFactorVerificationStatus>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Ok<ApiResponse<string>>>();
        var okResult = result as Ok<ApiResponse<string>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data.Should().Be("Two-factor verification status updated successfully.");
        okResult.Value?.TraceId.Should().Be("test-trace-id");

        var updatedUser = await dbContext.Users.FindAsync(user.Id);
        updatedUser!.TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Not_Authenticated()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var result = await UpdateUserTwoFactorVerificationStatus.Handle(
            true,
            CreateClaimsPrincipal(""),
            dbContext,
            CreateHttpContext(),
            NullLogger<UpdateUserTwoFactorVerificationStatus>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var result = await UpdateUserTwoFactorVerificationStatus.Handle(
            true,
            CreateClaimsPrincipal("nonexistent-id"),
            dbContext,
            CreateHttpContext(),
            NullLogger<UpdateUserTwoFactorVerificationStatus>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<NotFound<ApiResponse<string>>>();
        var notFoundResult = result as NotFound<ApiResponse<string>>;
        notFoundResult!.Value?.Success.Should().BeFalse();
        notFoundResult.Value?.Message.Should().Be("User not found.");
        notFoundResult.Value?.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Status_Already_Set()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var user = TestDataFactory.CreateUser("user1", "Test", "test@test.com", "testUser", "User");
        user.TwoFactorEnabled = true;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        var result = await UpdateUserTwoFactorVerificationStatus.Handle(
            true,
            CreateClaimsPrincipal(user.Id),
            dbContext,
            CreateHttpContext(),
            NullLogger<UpdateUserTwoFactorVerificationStatus>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
        var badRequestResult = result as BadRequest<ApiResponse<string>>;
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("Two-factor verification status is already set to the specified value.");
        badRequestResult.Value?.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Disable_TwoFactor_When_Flag_Is_False()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var user = TestDataFactory.CreateUser("user1", "Test", "test@test.com", "testUser", "User");
        user.TwoFactorEnabled = true;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        var result = await UpdateUserTwoFactorVerificationStatus.Handle(
            false,
            CreateClaimsPrincipal(user.Id),
            dbContext,
            CreateHttpContext(),
            NullLogger<UpdateUserTwoFactorVerificationStatus>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Ok<ApiResponse<string>>>();
        var okResult = result as Ok<ApiResponse<string>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.TraceId.Should().Be("test-trace-id");

        var updatedUser = await dbContext.Users.FindAsync(user.Id);
        updatedUser!.TwoFactorEnabled.Should().BeFalse();
    }
}

