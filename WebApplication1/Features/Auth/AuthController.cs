using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Features.Auth;

// zmienić rodzaj zwracanych danych żeby nie było zwracanego tokena i refresh tokena w odpowiedzi
[Controller]
[Route("api/[controller]")]
public class AuthController(
    UserManager<User> userManager,
    AppDbContext dbContext,
    IAuthService authorizationService)
    : ControllerBase
{

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        
        if (user == null )
        {
            return Unauthorized(new { Message = "Invalid username or password." });
        }
        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { Message = "Invalid username or password. 1" });
        }

        var (token, refreshToken) = await authorizationService.GenerateTokensAsync(user);
        
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
        return Ok(user.Id);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRequestDto request)
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
            return BadRequest("Password must be at least 6 characters long.");
        }
        var emailExists = await dbContext.Users.AnyAsync(u => u.Email == user.Email);
        if (emailExists)
        {
            throw new ArgumentException("Email already exists.");
        }
        if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Surname) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            throw new ArgumentException("All fields are required.");
        }
        var result = await userManager.CreateAsync(user, request.Password);
        return result.Succeeded ? Ok() : BadRequest(result.Errors);
    }
    
    [Authorize("RefreshTokenPolicy")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request,CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken: cancellationToken);

        if (token is null || token.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized();
        }
        var user = await userManager.FindByIdAsync(token.UserId);
        if (user is null)
        {
            return Unauthorized();
        }
        token.IsRevoked = true;
        dbContext.RefreshTokens.Update(token);
        await dbContext.SaveChangesAsync(cancellationToken);
        var(newJwt, newRefreshToken) = await authorizationService.GenerateTokensAsync(user);
        return Ok(user.Id);
    }
    
    [HttpGet("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var accessToken = Request.Cookies["access_token"];
        var refreshToken = Request.Cookies["refresh_token"];
        
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest("No tokens found.");
        }

        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t =>
            t.Token == refreshToken, cancellationToken: cancellationToken);
        if (token is null)
        {
            return BadRequest("Invalid refresh token.");
        }

        token.IsRevoked = true;
        dbContext.RefreshTokens.Update(token);
        await dbContext.SaveChangesAsync(cancellationToken);

        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");

        return Ok("Logged out successfully.");
    }
    
    [HttpGet("secret")]
    [Authorize]
    public IActionResult Secret()
    {
        return Ok("This is a secret message only for authenticated users.");
    }
    
    [HttpPost("password-reset-request")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestCommand dto)
    {
        await authorizationService.GeneratePasswordResetTokenAsync(dto.Email);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }
    
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand dto)
    {
        var success = await authorizationService.ResetPasswordAsync(dto.Token, dto.NewPassword);

        if (!success)
            return BadRequest(new { message = "Invalid or expired token." });

        return Ok(new { message = "Password has been reset successfully." });
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