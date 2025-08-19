using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Features.Auth.Services;

public interface ITwoFactorService
{
    Task<string> GenerateCodeAsync(string userId, string? ipAddress = null, string? userAgent = null);
    Task<bool> ValidateCodeAsync(string userId, string code);
    Task InvalidateAllCodesAsync(string userId);
    Task<bool> HasValidCodeAsync(string userId);
    Task<TimeSpan> GetCodeExpiryTimeAsync(string userId);
}

public class TwoFactorService(AppDbContext context, ILogger<TwoFactorService> logger) : ITwoFactorService
{
    private readonly TimeSpan _codeValidityPeriod = TimeSpan.FromMinutes(5);

        public async Task<string> GenerateCodeAsync(string userId, string? ipAddress = null, string? userAgent = null)
        {
            await InvalidateAllCodesAsync(userId);
            
            var code = GenerateSecureCode();
            var expiresAt = DateTime.UtcNow.Add(_codeValidityPeriod);

            var twoFactorCode = new TwoFactorCode
            {
                UserId = userId,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsUsed = false,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            context.TwoFactorCodes.Add(twoFactorCode);
            await context.SaveChangesAsync();

            logger.LogInformation("2FA code generated for user {UserId} (expires at {ExpiresAt})", userId, expiresAt);

            return code;
        }

        public async Task<bool> ValidateCodeAsync(string userId, string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !IsNumeric(code))
            {
                logger.LogWarning("Invalid 2FA code format for user {UserId}", userId);
                return false;
            }

            var validCode = await context.TwoFactorCodes
                .FirstOrDefaultAsync(c => 
                    c.UserId == userId && 
                    c.Code == code && 
                    !c.IsUsed && 
                    c.ExpiresAt > DateTime.UtcNow);

            if (validCode == null)
            {
                logger.LogWarning("2FA code validation failed for user {UserId} - code not found or expired", userId);
                return false;
            }
            
            validCode.IsUsed = true;
            await context.SaveChangesAsync();

            logger.LogInformation("2FA code successfully validated for user {UserId}", userId);
            return true;
        }

        public async Task InvalidateAllCodesAsync(string userId)
        {
            var existingCodes = await context.TwoFactorCodes
                .Where(c => c.UserId == userId && !c.IsUsed)
                .ToListAsync();

            foreach (var code in existingCodes)
            {
                code.IsUsed = true;
            }

            if (existingCodes.Any())
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Invalidated {Count} existing 2FA codes for user {UserId}",
                    existingCodes.Count, userId);
            }
        }

        public async Task<bool> HasValidCodeAsync(string userId)
        {
            return await context.TwoFactorCodes
                .AnyAsync(c => 
                    c.UserId == userId && 
                    !c.IsUsed && 
                    c.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<TimeSpan> GetCodeExpiryTimeAsync(string userId)
        {
            var code = await context.TwoFactorCodes
                .Where(c => c.UserId == userId && !c.IsUsed && c.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(c => c.ExpiresAt)
                .FirstOrDefaultAsync();

            if (code == null)
                return TimeSpan.Zero;

            var remaining = code.ExpiresAt - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private static string GenerateSecureCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            
            var number = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
            return number.ToString("D6");
        }

        private static bool IsNumeric(string value)
        {
            return value.All(char.IsDigit);
        }
    }