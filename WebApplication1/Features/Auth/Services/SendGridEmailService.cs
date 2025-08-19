using System.Net;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace WebApplication1.Features.Auth.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
    Task SendTwoFactorCodeAsync(string email, string code, string userName = null);
}

public class SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger) : IEmailService
{
    private readonly string? _apiKey = configuration["SendGrid:ApiKey"];
    private readonly string? _senderEmail = configuration["SendGrid:SenderEmail"];
    private readonly string? _senderName = configuration["SendGrid:SenderName"];

    public async Task SendAsync(string to, string subject, string body)
    {
        var client = new SendGridClient(_apiKey);
        var from = new EmailAddress(_senderEmail, _senderName);
        var toEmail = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, body, body);

        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Body.ReadAsStringAsync();
            throw new Exception($"SendGrid error: {error}");
        }
    }
    
    public async Task SendTwoFactorCodeAsync(string email, string code, string userName = null)
    {
        try
        {
            var smtpSettings = configuration.GetSection("SmtpSettings");
                
            using var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]))
            {
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
                Credentials = new NetworkCredential(
    public Task SendTwoFactorCodeAsync(string email, string code, string userName = null)
    {
        throw new NotImplementedException("SendTwoFactorCodeAsync is not supported by SendGridEmailService. Use SmtpEmailService instead.");
    }
}

public class SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var smtpSettings = configuration.GetSection("SmtpSettings");
        using var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]))
        {
            EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
            Credentials = new NetworkCredential(
                smtpSettings["Username"],
                smtpSettings["Password"])
        };
        var message = new MailMessage(smtpSettings["FromEmail"], to, subject, body)
        {
            IsBodyHtml = true
        };
        await client.SendMailAsync(message);
        logger.LogInformation("SMTP email sent successfully.");
    }

    public async Task SendTwoFactorCodeAsync(string email, string code, string userName = null)
    {
        try
        {
            var smtpSettings = configuration.GetSection("SmtpSettings");

            using var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]))
            {
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
                Credentials = new NetworkCredential(
                    smtpSettings["Username"],
                    smtpSettings["Password"])
            };

            var subject = "Your verification code";
            var body = $@"
                    <h2>Two-Factor Authentication Code</h2>
                    <p>Hello {userName ?? "User"},</p>
                    <p>Your verification code is:</p>
                    <h1 style='color: #2196F3; font-family: monospace; letter-spacing: 5px;'>{code}</h1>
                    <p>This code will expire in 5 minutes.</p>
                    <p>If you didn't request this code, please ignore this email.</p>
                ";

            var message = new MailMessage(smtpSettings["FromEmail"], email, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(message);
                
            logger.LogInformation("2FA code email sent successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send 2FA code email.");
            throw;
        }
    }
}