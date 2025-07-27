namespace WebApplication1.Auth;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}