using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Context;

namespace WebApplication1.Auth;

public class AuthService : IAuthService
{
    private IConfiguration _configuration { get; }
    private DBContext _context { get; }
    private IEmailService _emailService { get;  }
    
    public AuthService(IConfiguration configuration,DBContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<(string token, string refreshToken)> GenerateTokensAsync(User.User user)
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

    public async Task GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
        
        if (user == null)
            return;
        
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken);
        var expiresAt = DateTime.UtcNow.AddHours(1);
        
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(user.Id),
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            Used = false
        };
        
        await _context.PasswordResetTokens.AddAsync(token);
        await _context.SaveChangesAsync();

        //zmienic na faktyczny url frontendu
        var resetLink = $"https://app/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var emailBody = $"Kliknij w link, aby zresetować hasło: {resetLink}";
        
        await _emailService.SendAsync(user.Email!, "Reset hasła", emailBody);
        
    }
}