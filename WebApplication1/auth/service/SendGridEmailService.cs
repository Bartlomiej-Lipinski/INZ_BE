using SendGrid;
using SendGrid.Helpers.Mail;

namespace WebApplication1.auth.service;

public class SendGridEmailService(IConfiguration configuration) : IEmailService
{
    private readonly string _apiKey = configuration["SendGrid:ApiKey"]!;
    private readonly string _senderEmail = configuration["SendGrid:SenderEmail"]!;
    private readonly string _senderName = configuration["SendGrid:SenderName"]!;

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
}