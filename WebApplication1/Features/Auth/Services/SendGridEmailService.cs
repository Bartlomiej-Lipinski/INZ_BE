namespace WebApplication1.Features.Auth.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
    Task SendTwoFactorCodeAsync(string email, string code, string? userName = null);
}