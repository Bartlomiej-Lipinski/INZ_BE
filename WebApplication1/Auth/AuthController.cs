using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Context;
using WebApplication1.Models;

namespace WebApplication1.Auth;

// zmienić rodzaj zwracanych danych żeby nie było zwracanego tokena i refresh tokena w odpowiedzi
[Controller]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IAuthService _authorizationService;

    public AuthController(UserManager<User> userManager, AppDbContext dbContext,
        IAuthService authorizationService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _authorizationService = authorizationService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { Message = "Invalid username or password." });
        }

        var (token, refreshToken) = await _authorizationService.GenerateTokensAsync(user);
        
        Response.Cookies.Append("access token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });
        Response.Cookies.Append("refresh token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(2)
            });
        return Ok(new{token, refreshToken = refreshToken});
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new User
        {
            Email = request.Email,
            PasswordHash = request.Password,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        return result.Succeeded ? Ok() : BadRequest(result.Errors);
    }
    [Authorize("RefreshTokenPolicy")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request,CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken: cancellationToken);
        if (token is null || token.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized();
        }
        var user = await _userManager.FindByIdAsync(token.UserId);
        if (user is null)
        {
            return Unauthorized();
        }
        token.IsRevoked = true;
        _dbContext.RefreshTokens.Update(token);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var(newJwt, newRefreshToken) = await _authorizationService.GenerateTokensAsync(user);
        return Ok(new{token = newJwt, refreshToken = newRefreshToken});
    }
    [HttpGet("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var accessToken = Request.Cookies["access token"];
        var refreshToken = Request.Cookies["refresh token"];
        
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest("No tokens found.");
        }

        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken: cancellationToken);
        if (token is null)
        {
            return BadRequest("Invalid refresh token.");
        }

        token.IsRevoked = true;
        _dbContext.RefreshTokens.Update(token);
        await _dbContext.SaveChangesAsync(cancellationToken);

        Response.Cookies.Delete("access token");
        Response.Cookies.Delete("refresh token");

        return Ok("Logged out successfully.");
    }
    
    [HttpGet("secret")]
    [Authorize]
    public IActionResult Secret()
    {
        return Ok("This is a secret message only for authenticated users.");
    }
}