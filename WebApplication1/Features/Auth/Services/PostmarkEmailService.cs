using PostmarkDotNet;
using WebApplication1.Features.Auth.Services;

internal sealed class PostmarkEmailService(IConfiguration configuration, ILogger<PostmarkEmailService> logger)
    : IEmailService
{
    private readonly string _apiKey = configuration["Postmark:ApiKey"] ??
                                      throw new InvalidOperationException("Postmark API key not configured");

    private readonly string _fromEmail = configuration["Postmark:FromEmail"] ??
                                         throw new InvalidOperationException("Postmark from email not configured");

    public async Task SendAsync(string to, string subject, string body)
    {
        try
        {
            var message = new PostmarkMessage
            {
                To = to,
                From = _fromEmail,
                TrackOpens = true,
                Subject = subject,
                TextBody = body,
                HtmlBody = $"<div>{body}</div>",
                MessageStream = "outbound"
            };

            var client = new PostmarkClient(_apiKey);
            var sendResult = await client.SendMessageAsync(message);

            if (sendResult.Status == PostmarkStatus.Success)
            {
                logger.LogInformation("Email sent successfully to {To} with subject {Subject}", to, subject);
            }
            else
            {
                logger.LogError("Failed to send email to {To}. Status: {Status}, Message: {Message}",
                    to, sendResult.Status, sendResult.Message);
                throw new InvalidOperationException($"Failed to send email: {sendResult.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {To}", to);
            throw;
        }
    }

    public async Task SendTwoFactorCodeAsync(string email, string code, string? userName = null)
    {
        try
        {
            var greeting = string.IsNullOrEmpty(userName) ? "Hello" : $"Hello {userName}";
            var subject = "Your Two-Factor Authentication Code";
            var textBody =
                $"{greeting},\n\nYour two-factor authentication code is: {code}\n\nThis code will expire in 10 minutes.\n\nIf you didn't request this code, please ignore this email.";
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2>Two-Factor Authentication</h2>
                    <p>{greeting},</p>
                    <p>Your two-factor authentication code is:</p>
                    <div style='background-color: #f5f5f5; padding: 20px; text-align: center; margin: 20px 0;'>
                        <span style='font-size: 24px; font-weight: bold; letter-spacing: 4px; color: #333;'>{code}</span>
                    </div>
                    <p>This code will expire in <strong>10 minutes</strong>.</p>
                    <p style='color: #666; font-size: 14px;'>If you didn't request this code, please ignore this email.</p>
                </div>";

            var message = new PostmarkMessage
            {
                To = email,
                From = _fromEmail,
                TrackOpens = false,
                Subject = subject,
                TextBody = textBody,
                HtmlBody = htmlBody,
                MessageStream = "outbound",
                Tag = "Two-Factor-Authentication"
            };

            var client = new PostmarkClient(_apiKey);
            var sendResult = await client.SendMessageAsync(message);

            if (sendResult.Status == PostmarkStatus.Success)
            {
                logger.LogInformation("Two-factor code sent successfully to {Email}", email);
            }
            else
            {
                logger.LogError("Failed to send two-factor code to {Email}. Status: {Status}, Message: {Message}",
                    email, sendResult.Status, sendResult.Message);
                throw new InvalidOperationException($"Failed to send two-factor code: {sendResult.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending two-factor code to {Email}", email);
            throw;
        }
    }
}