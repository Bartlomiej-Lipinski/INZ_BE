using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Mates.Features.Auth;
using Mates.Features.Auth.Services;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities;

namespace Mates.Tests.Features.Auth;

public class AuthControllerTest : TestBase
{
    private static AuthController CreateController(AppDbContext dbContext,
        out Mock<UserManager<User>> userManagerMock,
        out Mock<IAuthService> authServiceMock,
        out Mock<ILoginAttemptService> loginAttemptMock,
        out Mock<ICaptchaService> captchaMock,
        out Mock<ITwoFactorService> twoFactorMock,
        out Mock<IEmailService> emailMock,
        out Mock<ILogger<AuthController>> loggerMock)
    {
        userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        authServiceMock = new Mock<IAuthService>();
        loginAttemptMock = new Mock<ILoginAttemptService>();
        captchaMock = new Mock<ICaptchaService>();
        twoFactorMock = new Mock<ITwoFactorService>();
        emailMock = new Mock<IEmailService>();
        loggerMock = new Mock<ILogger<AuthController>>();
        
        var controller = new AuthController(
            userManagerMock.Object,
            dbContext,
            authServiceMock.Object,
            loginAttemptMock.Object,
            captchaMock.Object,
            twoFactorMock.Object,
            emailMock.Object,
            loggerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "test-trace-id"
                }
            }
        };
        
        return controller;
    }
    
    [Fact]
    public async Task Register_Should_Create_User_When_Valid()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var controller = CreateController(dbContext, out var userManagerMock, out _,
            out _, out _, out _, out _, out _);

        userManagerMock.Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await controller.Register(
            TestDataFactory.CreateUserRequestDto(
                "test@test.com", "testUser", "password123", "Test", "User"));
        
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }
    
    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Password_Too_Short()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var controller = CreateController(dbContext, out _, out _,
            out _, out _, out _, out _, out _);

        var result = await controller.Register(
            TestDataFactory.CreateUserRequestDto(
                "test@test.com", "testUser", "123", "Test", "User"));
        
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().NotBeNull();
        
        var jsonResponse = JsonConvert.SerializeObject(badRequest.Value);
        jsonResponse.Should().Contain("test-trace-id");
    }
    
    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_Invalid_Password()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var controller = CreateController(dbContext, out var userManagerMock, out _,
            out var loginAttemptMock, out _, out _, out _, out _);
        
        var user = new User { Id = "1", Email = "test@test.com" };
        
        userManagerMock.Setup(u => u.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(u => u.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);
        loginAttemptMock.Setup(
            l => 
                l.RequiresCaptchaAsync("test@test.com", It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(false);
        loginAttemptMock.Setup(
            l => 
                l.RecordAttemptAsync("test@test.com", It.IsAny<string>(), false))
            .Returns(Task.CompletedTask);
        
        var result = await controller.Login(
            TestDataFactory.CreateExtendedLoginRequest("test@test.com", "wrong"));

        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorized = result as UnauthorizedObjectResult;
        unauthorized!.Value.Should().NotBeNull();
        
        var jsonResponse = JsonConvert.SerializeObject(unauthorized.Value);
        jsonResponse.Should().Contain("test-trace-id");
    }
    
    [Fact]
    public async Task Login_Should_Trigger_2FA_When_Enabled()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var controller = CreateController(dbContext, out var userManagerMock, out _,
            out var loginAttemptMock, out _, out var twoFactorMock, out _, out _);
        
        var user = TestDataFactory.CreateUser("1", "test@test.com", "testUser");
        userManagerMock.Setup(u => u.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(u => u.CheckPasswordAsync(user, "password123")).ReturnsAsync(true);
        userManagerMock.Setup(u => u.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
        loginAttemptMock.Setup(
            l => 
                l.RequiresCaptchaAsync(user.Email!, It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(false);
        loginAttemptMock.Setup(
            l => l.RecordAttemptAsync(user.Email!, It.IsAny<string>(), true))
            .Returns(Task.CompletedTask);
        
        twoFactorMock.Setup(
            t => t.GenerateCodeAsync(user.Id, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("123456");
        twoFactorMock.Setup(
            t => t.GetCodeExpiryTimeAsync(user.Id)).ReturnsAsync(TimeSpan.FromMinutes(5));
        
        var result = await controller.Login(new ExtendedLoginRequest { Email = user.Email!, Password = "password123" });
        
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();

        var jsonResponse = JsonConvert.SerializeObject(okResult.Value);
        jsonResponse.Should().Contain("test-trace-id");
    }
}