using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Context;
using WebApplication1.Models;

namespace WebApplication1.Auth;

public class AuthService : IAuthService
{
    private IConfiguration _configuration { get; }
    private AppDbContext _context { get; }
    
    public AuthService(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<(string token, string refreshToken)> GenerateTokensAsync(User user)
    {
        // Implementation for generating tokens
        // This is a placeholder implementation
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Auth:Key"]));
        var token = new JwtSecurityToken(claims: claims,expires:DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = new RefreshToken()
        {
            Token = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
            IsRevoked = false
        };
        _context.RefreshTokens.Add(refresh);
        await _context.SaveChangesAsync();
        return (jwt, refresh.Token);
    }
}