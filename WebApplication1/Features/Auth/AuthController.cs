using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Auth.Services;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Features.Auth;

// zmienić rodzaj zwracanych danych żeby nie było zwracanego tokena i refresh tokena w odpowiedzi
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
            return BadRequest(ModelState);
        }

        var userIp = GetUserIpAddress();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();
        
        logger.LogInformation("Login attempt. TraceId: {TraceId}", traceId);
        
        try
        {
            var requiresCaptcha = await loginAttemptService.RequiresCaptchaAsync(request.Email, userIp);
            
            if (requiresCaptcha)
            {
                if (string.IsNullOrEmpty(request.CaptchaToken))
                {
                    logger.LogWarning("CAPTCHA required. TraceId: {TraceId}", traceId);
                    return BadRequest(new {
                        error = "CAPTCHA_REQUIRED", 
                        message = "CAPTCHA verification required due to multiple failed login attempts",
                        requiresCaptcha = true,
                        traceId
                    });
                }

                var isCaptchaValid = await captchaService.ValidateCaptchaAsync(request.CaptchaToken, userIp);
                if (!isCaptchaValid)
                {
                    await loginAttemptService.RecordAttemptAsync(request.Email, userIp, false);
                    logger.LogWarning("Invalid CAPTCHA. TraceId: {TraceId}", traceId);
                    return StatusCode(403, new {
                        error = "CAPTCHA_INVALID", 
                        message = "Invalid CAPTCHA verification",
                        requiresCaptcha = true,
                        traceId
                    });
                }
            }
            
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                await loginAttemptService.RecordAttemptAsync(request.Email, userIp, false);
                var newRequiresCaptcha = await loginAttemptService.RequiresCaptchaAsync(request.Email, userIp);
                
                logger.LogWarning("Failed login attempt. TraceId: {TraceId}", traceId);
                return Unauthorized(new {
                    Message = "Invalid username or password.",
                    requiresCaptcha = newRequiresCaptcha,
                    traceId
                });
            }
            
            var has2Fa = await userManager.GetTwoFactorEnabledAsync(user);
                
            if (has2Fa)
            {
                var code = await twoFactorService.GenerateCodeAsync(user.Id, userIp, userAgent);

                if (user.Email != null) await emailService.SendTwoFactorCodeAsync(user.Email, code, user.UserName);

                await loginAttemptService.RecordAttemptAsync(request.Email, userIp, true);
                
                logger.LogInformation("2FA code sent for user: {UserId}. TraceId: {TraceId}", user.Id, traceId);
                return Ok(new { 
                    requiresTwoFactor = true,
                    message = "Verification code sent to your email",
                    expiresIn = (int)(await twoFactorService.GetCodeExpiryTimeAsync(user.Id)).TotalSeconds,
                    traceId
                });
            }

            var (token, refreshToken) = await authorizationService.GenerateTokensAsync(user);
            
            await loginAttemptService.ResetFailedAttemptsAsync(request.Email, userIp);
            await loginAttemptService.RecordAttemptAsync(request.Email, userIp, true);

            SetAuthCookies(token, refreshToken);
            
            logger.LogInformation("Successful login for user: {UserId}. TraceId: {TraceId}", user.Id, traceId);
            return Ok(user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login attempt. TraceId: {TraceId}", traceId);
            return StatusCode(500, new { 
                error = "INTERNAL_ERROR", 
                message = "An error occurred during login",
                traceId
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody]UserRequestDto request)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        
        logger.LogInformation("Registration attempt. TraceId: {TraceId}", traceId);
        
        try
        {
            var user = new User
            {
                Name = request.Name,
                UserName = request.UserName,
                Surname = request.Surname,
                Email = request.Email,
                BirthDate = request.BirthDate,
            };
            
            if (request.Password is null || request.Password.Length < 6)
            {
                logger.LogWarning("Invalid password length for registration. TraceId: {TraceId}", traceId);
                return BadRequest(new { message = "Password must be at least 6 characters long.", traceId });
            }
            
            var emailExists = await dbContext.Users.AnyAsync(u => u.Email == user.Email);
            if (emailExists)
            {
                logger.LogWarning("Registration attempt with existing email. TraceId: {TraceId}", traceId);
                return BadRequest(new { message = "Email already exists.", traceId });
            }
            
            if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Surname) || 
                string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                logger.LogWarning("Missing required fields for registration. TraceId: {TraceId}", traceId);
                return BadRequest(new { message = "All fields are required.", traceId });
            }
            
            var result = await userManager.CreateAsync(user, request.Password);
            
            if (result.Succeeded)
            {
                logger.LogInformation("User successfully registered with ID: {UserId}. TraceId: {TraceId}", 
                    user.Id, traceId);
                return Ok(user.Id);
            }
            else
            {
                logger.LogWarning("Registration failed. Errors: {Errors}. TraceId: {TraceId}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)), traceId);
                return BadRequest(new { errors = result.Errors, traceId });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration. TraceId: {TraceId}", traceId);
            return StatusCode(500, new { 
                message = "An error occurred during registration", traceId 
            });
        }
    }
    
    // [Authorize("RefreshTokenPolicy")]
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        
        logger.LogInformation("Token refresh attempt. TraceId: {TraceId}", traceId);
        
        try
        {
            var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken, 
                cancellationToken: cancellationToken);

            if (token is null || token.ExpiresAt < DateTime.UtcNow)
            {
                logger.LogWarning("Invalid or expired refresh token. TraceId: {TraceId}", traceId);
                return Unauthorized(new { message = "Invalid or expired refresh token", traceId });
            }
            
            var user = await userManager.FindByIdAsync(token.UserId);
            if (user is null)
            {
                logger.LogWarning("User not found for refresh token. UserId: {UserId}. TraceId: {TraceId}", 
                    token.UserId, traceId);
                return Unauthorized(new { message = "User not found", traceId });
            }
            
            token.IsRevoked = true;
            dbContext.RefreshTokens.Update(token);
            await dbContext.SaveChangesAsync(cancellationToken);
            var (newAccessToken, refreshToken) = await authorizationService.GenerateTokensAsync(user);
            SetAuthCookies(newAccessToken, refreshToken);
            
            logger.LogInformation("Token successfully refreshed for user: {UserId}. TraceId: {TraceId}", 
                user.Id, traceId);
            return Ok(user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh. TraceId: {TraceId}", traceId);
            return StatusCode(500, new { 
                message = "An error occurred during token refresh", traceId 
            });
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
                return BadRequest(new { message = "No tokens found.", traceId });
            }

            var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t =>
                t.Token == refreshToken, cancellationToken: cancellationToken);
            if (token is null)
            {
                logger.LogWarning("Invalid refresh token for logout. TraceId: {TraceId}", traceId);
                return BadRequest(new { message = "Invalid refresh token.", traceId });
            }

            token.IsRevoked = true;
            dbContext.RefreshTokens.Update(token);
            await dbContext.SaveChangesAsync(cancellationToken);

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");

            logger.LogInformation("User successfully logged out. TraceId: {TraceId}", traceId);
            return Ok(new { message = "Logged out successfully.", traceId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout. TraceId: {TraceId}", traceId);
            return StatusCode(500, new { 
                message = "An error occurred during logout", traceId 
            });
        }
    }
    
    [Authorize]
    [HttpGet("secret")]
    public IActionResult Secret()
    {
        return Ok("This is a secret message only for authenticated users.");
    }
    
    [AllowAnonymous]
    [HttpPost("password-reset-request")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest dto)
    {
        await authorizationService.GeneratePasswordResetTokenAsync(dto.Email);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }
    
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordResponse dto)
    {
        var success = await authorizationService.ResetPasswordAsync(dto.Token, dto.NewPassword);

        if (!success)
            return BadRequest(new { message = "Invalid or expired token." });

        return Ok(new { message = "Password has been reset successfully." });
    }
    
    [HttpGet("captcha-required")]
    public async Task<IActionResult> CheckCaptchaRequired([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required");
        }

        var userIp = GetUserIpAddress();
        var requiresCaptcha = await loginAttemptService.RequiresCaptchaAsync(email, userIp);
            
        return Ok(new { requiresCaptcha });
    }
    
    [AllowAnonymous]
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        
        logger.LogInformation("2FA verification attempt. TraceId: {TraceId}", traceId);
        
        try
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                logger.LogWarning("Invalid 2FA verification attempt. User not found. TraceId: {TraceId}", traceId);
                return Unauthorized(new { Message = "Invalid request." });
            }

            var isValidCode = await twoFactorService.ValidateCodeAsync(user.Id, request.Code);
                
            if (!isValidCode)
            {
                logger.LogWarning("Invalid or expired 2FA code. TraceId: {TraceId}", traceId);
                return Unauthorized(new { 
                    Message = "Invalid or expired verification code.",
                    codeExpired = !await twoFactorService.HasValidCodeAsync(user.Id),
                    traceId
                });
            }
            
            var (token, refreshToken) = await authorizationService.GenerateTokensAsync(user);
                
            SetAuthCookies(token, refreshToken);
                
            logger.LogInformation("2FA verification successful for user: {UserId}. TraceId: {TraceId}", user.Id, traceId);
            return Ok(user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during 2FA verification. TraceId: {TraceId}", traceId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred during verification" });
        }
    }
    
    [AllowAnonymous]
    [HttpPost("resend-2fa")]
    public async Task<IActionResult> ResendTwoFactorCode([FromBody] TwoFactorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        
        logger.LogInformation("Resend 2FA code attempt. TraceId: {TraceId}", traceId);
        
        try
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                logger.LogWarning("Resend 2FA code attempt for non-existing user. TraceId: {TraceId}", traceId);
                return Ok(new { message = "If the email exists, a new code has been sent." });
            }

            var userIp = GetUserIpAddress();
            var userAgent = Request.Headers.UserAgent.FirstOrDefault();
            
            var code = await twoFactorService.GenerateCodeAsync(user.Id, userIp, userAgent);
            if (user.Email != null)
                await emailService.SendTwoFactorCodeAsync(user.Email, code, user.UserName);

            logger.LogInformation("New 2FA code sent. TraceId: {TraceId}", traceId);
            return Ok(new { 
                message = "New verification code sent",
                expiresIn = (int)(await twoFactorService.GetCodeExpiryTimeAsync(user.Id)).TotalSeconds,
                traceId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during 2FA code resend. TraceId: {TraceId}", traceId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred" });
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
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(15)
        });
                
        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(2)
        });
    }
    
    public class UserRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
        [Required]
        public string UserName { get; set; } = null!;
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? Photo { get; set; }
        public string? Password { get; set; }
    }
}