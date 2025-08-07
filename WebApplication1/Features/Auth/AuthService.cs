using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Features.Auth;

public interface IAuthService
{
    Task<(string token, string refreshToken)> GenerateTokensAsync(User user);
    Task GeneratePasswordResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}

public class AuthService(IConfiguration configuration, AppDbContext context, IEmailService emailService) : IAuthService
{
    public async Task<(string token, string refreshToken)> GenerateTokensAsync(User user)
    {
        // Implementation for generating tokens
        // This is a placeholder implementation
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Auth:Key"]));
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
            IsRevoked = false
        };
        context.RefreshTokens.Add(refresh);
        await context.SaveChangesAsync();
        return (jwt, refresh.Token);
    }

    public async Task GeneratePasswordResetTokenAsync(string email)
    {
        var user = await context.Users.SingleOrDefaultAsync(u => u.Email == email);

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
            IsUsed = false
        };

        await context.PasswordResetTokens.AddAsync(token);
        await context.SaveChangesAsync();

        //zmienic na faktyczny url frontendu
        var resetLink = $"https://app/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var emailBody = $"Kliknij w link, aby zresetować hasło: {resetLink}";

        await emailService.SendAsync(user.Email!, "Reset hasła", emailBody);
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var tokenRecord = await context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

        if (tokenRecord == null || !BCrypt.Net.BCrypt.Verify(token, tokenRecord.TokenHash))
            return false;

        var user = tokenRecord.User;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        tokenRecord.IsUsed = true;

        await context.SaveChangesAsync();
        return true;
    }
}