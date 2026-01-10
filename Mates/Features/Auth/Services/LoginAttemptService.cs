using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mates.Features.Auth.Services;

public interface ILoginAttemptService
{
    Task<int> GetFailedAttemptsCountAsync(
        string email, string ipAddress, TimeSpan timeWindow, CancellationToken cancellationToken);
    Task RecordAttemptAsync(string email, string ipAddress, bool isSuccessful);
    Task ResetFailedAttemptsAsync(string email, string ipAddress, CancellationToken cancellationToken);
    Task<bool> RequiresCaptchaAsync(string email, string ipAddress, CancellationToken cancellationToken);
}

internal sealed class LoginAttemptService(AppDbContext context) : ILoginAttemptService
{
    private const int MaxFailedAttempts = 3;
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(30);

    public async Task<int> GetFailedAttemptsCountAsync(
        string email, string ipAddress, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        
        return await context.LoginAttempts
            .Where(l => EF.Functions.ILike(l.Email, email)
                        && l.IpAddress == ipAddress
                        && !l.IsSuccessful
                        && l.AttemptTime >= cutoffTime)
            .CountAsync(cancellationToken);
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

    public async Task ResetFailedAttemptsAsync(
        string email, string ipAddress, CancellationToken cancellationToken = default)
    {
        await context.LoginAttempts
            .Where(l => EF.Functions.ILike(l.Email, email)
                        && l.IpAddress == ipAddress
                        && !l.IsSuccessful)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> RequiresCaptchaAsync(
        string email, string ipAddress, CancellationToken cancellationToken = default)
    {
        var failedCount = await GetFailedAttemptsCountAsync(email, ipAddress, _timeWindow, cancellationToken);
        return failedCount >= MaxFailedAttempts;
    }
}