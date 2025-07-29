namespace WebApplication1.auth.service;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}