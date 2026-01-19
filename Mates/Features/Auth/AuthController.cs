using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Mates.Features.Auth.Services;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities;
using Mates.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Mates.Features.Auth;

[EnableRateLimiting("AuthPolicy")]
[Controller]
[Route("api/[controller]")]
public class AuthController(
    UserManager<User> userManager,
    AppDbContext dbContext,
    IAuthService authorizationService,
    ILoginAttemptService loginAttemptService,
    ICaptchaService captchaService,
    ITwoFactorService twoFactorService,
    IEmailService emailService,
    ILogger<AuthController> logger)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] ExtendedLoginRequest request)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (!ModelState.IsValid)
        {
            logger.LogWarning("Invalid login request data. TraceId: {TraceId}", traceId);
            return BadRequest(ApiResponse<string>.Fail("Invalid request data", traceId));
        }

        var userIp = GetUserIpAddress();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        logger.LogInformation("Login attempt. TraceId: {TraceId}", traceId);

        try
        {
            var requiresCaptcha = await loginAttemptService
                .RequiresCaptchaAsync(request.Email, userIp, HttpContext.RequestAborted);

            switch (requiresCaptcha)
            {
                case true when string.IsNullOrEmpty(request.CaptchaToken):
                    return BadRequest(ApiResponse<string>
                        .Fail("CAPTCHA verification required due to multiple failed login attempts", traceId));
                case true when !await captchaService.ValidateCaptchaAsync(request.CaptchaToken, userIp):
                    await loginAttemptService.RecordAttemptAsync(request.Email, userIp, false);
                    return StatusCode(403, ApiResponse<string>.Fail("Invalid CAPTCHA verification", traceId));
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                await loginAttemptService.RecordAttemptAsync(request.Email, userIp, false);
                return Unauthorized(ApiResponse<string>.Fail("Invalid username or password", traceId));
            }

            if (await userManager.GetTwoFactorEnabledAsync(user))
            {
                var code = await twoFactorService.GenerateCodeAsync(user.Id, userIp, userAgent);
                if (user.Email != null) await emailService.SendTwoFactorCodeAsync(user.Email, code, user.UserName);
                return Ok(ApiResponse<string>.Ok("Verification code sent to your email", traceId: traceId));
            }

            var (token, refreshToken) = await authorizationService.GenerateTokensAsync(user);
            await loginAttemptService.ResetFailedAttemptsAsync(request.Email, userIp, HttpContext.RequestAborted);
            SetAuthCookies(token, refreshToken);

            return Ok(ApiResponse<string>.Ok(user.Id, "Login successful", traceId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login attempt. TraceId: {TraceId}", traceId);
            return StatusCode(500, ApiResponse<string>.Fail("An error occurred during login", traceId));
        }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRequestDto request)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        logger.LogInformation("Registration attempt. TraceId: {TraceId}", traceId);

        try
        {
            if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Surname) ||
                string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                logger.LogWarning("Missing required fields for registration. TraceId: {TraceId}", traceId);
                return BadRequest(ApiResponse<string>.Fail("All fields are required.", traceId));
            }

            if (request.Password.Length < 8)
            {
                logger.LogWarning("Invalid password length for registration. TraceId: {TraceId}", traceId);
                return BadRequest(ApiResponse<string>.Fail("Password must be at least 8 characters long.", traceId));
            }

            var emailExists = await dbContext.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                logger.LogWarning("Registration attempt with existing email. TraceId: {TraceId}", traceId);
                return BadRequest(ApiResponse<string>.Fail("Email already exists.", traceId));
            }

            var user = new User
            {
                Name = request.Name,
                UserName = RemoveDiacritics(request.UserName).ToLowerInvariant(),
                Surname = request.Surname,
                Email = request.Email,
                BirthDate = request.BirthDate,
                TwoFactorEnabled = false
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                logger.LogInformation("User successfully registered with ID: {UserId}. TraceId: {TraceId}",
                    user.Id, traceId);
                return Ok(ApiResponse<string>.Ok(user.Id, "User registered successfully", traceId));
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Registration failed. Errors: {Errors}. TraceId: {TraceId}", errors, traceId);
            return BadRequest(ApiResponse<string>.Fail(errors, traceId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration. TraceId: {TraceId}", traceId);
            return StatusCode(500, ApiResponse<string>.Fail("An error occurred during registration", traceId));
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        logger.LogInformation("Token refresh attempt. TraceId: {TraceId}", traceId);

        try
        {
            var token = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

            if (token is null || token.ExpiresAt < DateTime.UtcNow)
            {
                logger.LogWarning("Invalid or expired refresh token. TraceId: {TraceId}", traceId);
                return Unauthorized(ApiResponse<string>.Fail("Invalid or expired refresh token", traceId));
            }

            var user = await userManager.FindByIdAsync(token.UserId);
            if (user is null)
            {
                logger.LogWarning("User not found for refresh token. UserId: {UserId}. TraceId: {TraceId}",
                    token.UserId, traceId);
                return Unauthorized(ApiResponse<string>.Fail("User not found", traceId));
            }

            token.IsRevoked = true;
            dbContext.RefreshTokens.Update(token);
            await dbContext.SaveChangesAsync(cancellationToken);

            var (newAccessToken, newRefreshToken) = await authorizationService.GenerateTokensAsync(user);
            SetAuthCookies(newAccessToken, newRefreshToken);

            logger.LogInformation("Token successfully refreshed for user: {UserId}. TraceId: {TraceId}",
                user.Id, traceId);

            return Ok(ApiResponse<string>.Ok(user.Id, "Token refreshed successfully", traceId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh. TraceId: {TraceId}", traceId);
            return StatusCode(500, ApiResponse<string>.Fail("An error occurred during token refresh", traceId));
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        logger.LogInformation("Logout attempt. TraceId: {TraceId}", traceId);

        try
        {
            var accessToken = Request.Cookies["access_token"];
            var refreshToken = Request.Cookies["refresh_token"];

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                logger.LogWarning("No tokens found for logout. TraceId: {TraceId}", traceId);
                return BadRequest(ApiResponse<string>.Fail("No tokens found.", traceId));
            }

            var token = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken: cancellationToken);
            if (token is null)
            {
                logger.LogWarning("Invalid refresh token for logout. TraceId: {TraceId}", traceId);
                return BadRequest(ApiResponse<string>.Fail("Invalid refresh token.", traceId));
            }

            token.IsRevoked = true;
            dbContext.RefreshTokens.Update(token);
            await dbContext.SaveChangesAsync(cancellationToken);

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");

            logger.LogInformation("User successfully logged out. TraceId: {TraceId}", traceId);
            return Ok(ApiResponse<string>.Ok("Logged out successfully.", traceId: traceId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout. TraceId: {TraceId}", traceId);
            return StatusCode(500, ApiResponse<string>.Fail("An error occurred during logout", traceId));
        }
    }

    [Authorize]
    [HttpGet("secret")]
    public IActionResult Secret()
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return Ok(ApiResponse<string>
            .Ok("This is a secret message only for authenticated users.", traceId: traceId));
    }

    [AllowAnonymous]
    [HttpPost("password-reset-request")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest dto)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        await authorizationService.GeneratePasswordResetTokenAsync(dto.Email);
        var user = await userManager.FindByEmailAsync(dto.Email);

        if (user == null)
            return Ok(ApiResponse<string>.Ok("If the email exists, a reset link has been sent.", traceId: traceId));
        await userManager.UpdateSecurityStampAsync(user);
        var tokens = dbContext.RefreshTokens.Where(t => t.UserId == user.Id && !t.IsRevoked);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await dbContext.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("If the email exists, a reset link has been sent.", traceId: traceId));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordResponse dto)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        var validation = ValidatePassword(dto.NewPassword);
        if (!string.IsNullOrEmpty(validation))
            return BadRequest(ApiResponse<string>.Fail(validation, traceId));

        var success = await authorizationService.ResetPasswordAsync(dto.Token, dto.NewPassword);
        if (!success)
            return BadRequest(ApiResponse<string>.Fail("Invalid or expired token.", traceId));

        await dbContext.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Password has been reset successfully.", traceId: traceId));
    }

    [HttpGet("captcha-required")]
    public async Task<IActionResult> CheckCaptchaRequired([FromQuery] string email)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(ApiResponse<string>.Fail("Email is required", traceId));
        }

        var userIp = GetUserIpAddress();
        var requiresCaptcha = await loginAttemptService
            .RequiresCaptchaAsync(email, userIp, HttpContext.RequestAborted);

        return Ok(ApiResponse<bool>.Ok(requiresCaptcha, traceId: traceId));
    }

    [AllowAnonymous]
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid request data"));
        }

        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        logger.LogInformation("2FA verification attempt. TraceId: {TraceId}", traceId);

        try
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                logger.LogWarning("Invalid 2FA verification attempt. User not found. TraceId: {TraceId}", traceId);
                return Unauthorized(ApiResponse<string>.Fail("Invalid request.", traceId));
            }

            var isValidCode = await twoFactorService.ValidateCodeAsync(user.Id, request.Code);

            if (!isValidCode)
            {
                logger.LogWarning("Invalid or expired 2FA code. TraceId: {TraceId}", traceId);
                return Unauthorized(ApiResponse<string>.Fail("Invalid or expired verification code.", traceId));
            }

            var (token, refreshToken) = await authorizationService.GenerateTokensAsync(user);
            SetAuthCookies(token, refreshToken);

            logger.LogInformation("2FA verification successful for user: {UserId}. TraceId: {TraceId}", user.Id,
                traceId);
            return Ok(ApiResponse<string>.Ok(user.Id, "2FA verification successful", traceId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during 2FA verification. TraceId: {TraceId}", traceId);
            return StatusCode(500, ApiResponse<string>.Fail("An error occurred during verification", traceId));
        }
    }

    [AllowAnonymous]
    [HttpPost("resend-2fa")]
    public async Task<IActionResult> ResendTwoFactorCode([FromBody] TwoFactorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid request data"));
        }

        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        logger.LogInformation("Resend 2FA code attempt. TraceId: {TraceId}", traceId);

        try
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                logger.LogWarning("Resend 2FA code attempt for non-existing user. TraceId: {TraceId}", traceId);
                return Ok(ApiResponse<string>
                    .Ok(null!, "If the email exists, a new code has been sent.", traceId));
            }

            var userIp = GetUserIpAddress();
            var userAgent = Request.Headers.UserAgent.FirstOrDefault();

            var code = await twoFactorService.GenerateCodeAsync(user.Id, userIp, userAgent);
            if (user.Email != null)
                await emailService.SendTwoFactorCodeAsync(user.Email, code, user.UserName);

            logger.LogInformation("New 2FA code sent. TraceId: {TraceId}", traceId);

            var expiresIn = (int)(await twoFactorService.GetCodeExpiryTimeAsync(user.Id)).TotalSeconds;
            return Ok(ApiResponse<int>.Ok(expiresIn, "New verification code sent", traceId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during 2FA code resend. TraceId: {TraceId}", traceId);
            return StatusCode(500, ApiResponse<string>.Fail("An error occurred during 2FA code resend", traceId));
        }
    }

    private string GetUserIpAddress()
    {
        return (Request.Headers.TryGetValue("X-Forwarded-For", out var value)
            ? value.FirstOrDefault()?.Split(',')[0].Trim()
            : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown") ?? string.Empty;
    }

    private void SetAuthCookies(string token, string refreshToken)
    {
        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(15),
            Domain = ".vercel.app"
        });

        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(2),
            Domain=".vercel.app"
        });
    }

    private static string ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return "Hasło musi mieć co najmniej 8 znaków.";

        if (!password.Any(char.IsUpper))
            return "Hasło musi zawierać co najmniej jedną wielką literę.";

        if (!password.Any(char.IsLower))
            return "Hasło musi zawierać co najmniej jedną małą literę.";

        if (!password.Any(char.IsDigit))
            return "Hasło musi zawierać co najmniej jedną cyfrę.";

        return password.All(char.IsLetterOrDigit)
            ? "Hasło musi zawierać co najmniej jeden znak specjalny."
            : string.Empty;
    }
    
    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalized = text.Normalize(NormalizationForm.FormD);
        return string.Concat(
            normalized.Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        ).Normalize(NormalizationForm.FormC);
    }

    public class UserRequestDto
    {
        [Required] [EmailAddress] public string Email { get; set; } = null!;

        [Required] public string UserName { get; set; } = null!;

        public string? Name { get; set; }
        public string? Surname { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? Photo { get; set; }
        public string? Password { get; set; }
    }
}