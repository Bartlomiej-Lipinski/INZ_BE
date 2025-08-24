using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Features.Auth;
using WebApplication1.Features.Auth.Services;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Tests.Features.Auth;

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
                HttpContext = new DefaultHttpContext()
            }
        };
        
        return controller;
    }
    
    [Fact]
    public async Task Register_Should_Create_User_When_Valid()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var controller = CreateController(dbContext, out var userManagerMock, out var authServiceMock,
            out var loginAttemptMock, out var captchaMock, out var twoFactorMock, out var emailMock, out var loggerMock);

        userManagerMock.Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await controller.Register(
            TestDataFactory.CreateUserRequestDto(
                "test@test.com", "testUser", "password123", "Test", "User"));
        
        result.Should().BeOfType<OkObjectResult>();
    }
    
    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_Invalid_Password()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var controller = CreateController(dbContext, out var userManagerMock, out var authServiceMock,
            out var loginAttemptMock, out var captchaMock, out var twoFactorMock, out var emailMock, out var loggerMock);
        
        var user = new User { Id = "1", Email = "test@test.com" };
        
        userManagerMock.Setup(u => u.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(u => u.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);
        loginAttemptMock.Setup(
            l => l.RequiresCaptchaAsync("test@test.com", It.IsAny<string>())).ReturnsAsync(false);
        
        var result = await controller.Login(
            TestDataFactory.CreateExtendedLoginRequest("test@test.com", "wrong"));

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
    
    [Fact]
    public async Task Login_Should_Trigger_2FA_When_Enabled()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var controller = CreateController(dbContext, out var userManagerMock, out var authServiceMock,
            out var loginAttemptMock, out var captchaMock, out var twoFactorMock, out var emailMock, out var loggerMock);
        
        var user = TestDataFactory.CreateUser("1", "test@test.com", "testUser");
        userManagerMock.Setup(u => u.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(u => u.CheckPasswordAsync(user, "password123")).ReturnsAsync(true);
        userManagerMock.Setup(u => u.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
        
        twoFactorMock.Setup(
            t => t.GenerateCodeAsync(user.Id, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("123456");
        
        var result = await controller.Login(new ExtendedLoginRequest { Email = user.Email!, Password = "password123" });
        result.Should().BeOfType<OkObjectResult>();
    }
}