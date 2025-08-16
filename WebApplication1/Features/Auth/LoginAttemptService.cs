using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Features.Auth;

public interface ILoginAttemptService
{
    Task<int> GetFailedAttemptsCountAsync(string email, string ipAddress, TimeSpan timeWindow);
    Task RecordAttemptAsync(string email, string ipAddress, bool isSuccessful);
    Task ResetFailedAttemptsAsync(string email, string ipAddress);
    Task<bool> RequiresCaptchaAsync(string email, string ipAddress);
}

public class LoginAttemptService(AppDbContext context) : ILoginAttemptService
{
    private const int MaxFailedAttempts = 3;
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(30);

    public async Task<int> GetFailedAttemptsCountAsync(string email, string ipAddress, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        
        return await context.LoginAttempts
            .Where(la => la.Email.Equals(email, StringComparison.OrdinalIgnoreCase) 
                         && la.IpAddress == ipAddress
                         && !la.IsSuccessful 
                         && la.AttemptTime >= cutoffTime)
            .CountAsync();
    }

    public async Task RecordAttemptAsync(string email, string ipAddress, bool isSuccessful)
    {
        var attempt = new LoginAttempt
        {
            Email = email.ToLower(),
            IpAddress = ipAddress,
            AttemptTime = DateTime.UtcNow,
            IsSuccessful = isSuccessful,
            CreatedAt = DateTime.UtcNow
        };

        context.LoginAttempts.Add(attempt);
        await context.SaveChangesAsync();
    }

    public async Task ResetFailedAttemptsAsync(string email, string ipAddress)
    {
        var failedAttempts = await context.LoginAttempts
            .Where(la => la.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && la.IpAddress == ipAddress
                         && !la.IsSuccessful)
            .ToListAsync();

        context.LoginAttempts.RemoveRange(failedAttempts);
        await context.SaveChangesAsync();
    }

    public async Task<bool> RequiresCaptchaAsync(string email, string ipAddress)
    {
        var failedCount = await GetFailedAttemptsCountAsync(email, ipAddress, _timeWindow);
        return failedCount >= MaxFailedAttempts;
    }
}