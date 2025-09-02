using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Features.Auth.Services;

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
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Auth:Key"]
                                                                  ?? throw new InvalidOperationException()));
        //TODO fine tune token expiration times
        var token = new JwtSecurityToken(claims: claims,expires:DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = new RefreshToken()
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
        {
            return;
        }
        
        var tokenId = Guid.NewGuid();
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var randomPart = Convert.ToBase64String(randomBytes);
        var rawToken = $"{tokenId}:{randomPart}";
        
        var tokenHash = ComputeSha256Base64(rawToken);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var tokenEntity = new PasswordResetToken
        {
            Id = tokenId.ToString(),
            TokenHash = tokenHash,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        await context.PasswordResetTokens.AddAsync(tokenEntity);
        await context.SaveChangesAsync();
        
        var resetLink = $"/reset-password?token={Uri.EscapeDataString(rawToken)}"; //TODO add frontend base url
        var emailBody = $"Kliknij w link, aby zresetować hasło:\n\n{resetLink}\n\nLink wygasa za 1 godzinę.";
        await emailService.SendAsync(user.Email!, "Reset hasła", emailBody);
    }
    
    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        
        var tokenHash = ComputeSha256Base64(token);
        
        var tokenRecord = await context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

        var user = tokenRecord?.User;
        if (user == null)
            return false;
        
        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);

        if (tokenRecord != null) tokenRecord.IsUsed = true;
        await context.SaveChangesAsync();
        return true;
    }

    private static string ComputeSha256Base64(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}