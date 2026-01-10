using DotNetEnv;
using PostmarkDotNet;

namespace Mates.Features.Auth.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
    Task SendTwoFactorCodeAsync(string email, string code, string? userName = null);
}

internal sealed class PostmarkEmailService(IConfiguration configuration, ILogger<PostmarkEmailService> logger)
    : IEmailService
{
    private readonly string _apiKey = Env.GetString("POSTMARK_API_KEY") ??
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
                logger.LogInformation("Email sent successfully with subject {Subject}", subject);
            }
            else
            {
                logger.LogError("Failed to send email. Status: {Status}, Message: {Message}",
                    sendResult.Status, sendResult.Message);
                throw new InvalidOperationException($"Failed to send email: {sendResult.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email");
            throw;
        }
    }

    public async Task SendTwoFactorCodeAsync(string email, string code, string? userName = null)
    {
        try
        {
            const string subject = "Your Two-Factor Authentication Code";
            var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Two-Factor Authentication Code</title>
            </head>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                    <h1 style='color: white; margin: 0; font-size: 28px;'>Two-Factor Authentication</h1>
                </div>
                <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;'>
                    <h2 style='color: #333; margin-top: 0;'>Hello {userName ?? "User"}!</h2>
                    <p style='font-size: 16px; color: #555;'>Your verification code is:</p>
                    
                    <div style='background: white; padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <h1 style='color: #2196F3; font-family: monospace; letter-spacing: 8px; font-size: 36px; margin: 0; font-weight: bold;'>{code}</h1>
                    </div>
                    
                    <div style='background: #fff3cd; border: 1px solid #ffeaa7; border-radius: 5px; padding: 15px; margin: 20px 0;'>
                        <p style='margin: 0; color: #856404;'>
                            <strong>⏰ This code will expire in 5 minutes.</strong>
                        </p>
                    </div>
                    
                    <p style='color: #666; font-size: 14px; margin-top: 30px;'>
                        If you didn't request this code, please ignore this email or contact our support team.
                    </p>
                    
                    <hr style='border: none; height: 1px; background: #eee; margin: 30px 0;'>
                    
                    <p style='color: #999; font-size: 12px; text-align: center;'>
                        This is an automated message, please do not reply to this email.
                    </p>
                </div>
            </body>
            </html>";

            var plainTextContent = $@"
            Two-Factor Authentication Code
            
            Hello {userName ?? "User"}!
            
            Your verification code is: {code}
            
            This code will expire in 5 minutes.
            
            If you didn't request this code, please ignore this email.";

            var message = new PostmarkMessage
            {
                To = email,
                From = _fromEmail,
                TrackOpens = false,
                Subject = subject,
                TextBody = plainTextContent,
                HtmlBody = htmlContent,
                MessageStream = "outbound",
                Tag = "Two-Factor-Authentication"
            };

            var client = new PostmarkClient(_apiKey);
            var sendResult = await client.SendMessageAsync(message);

            if (sendResult.Status == PostmarkStatus.Success)
            {
                logger.LogInformation("Two-factor code sent successfully");
            }
            else
            {
                logger.LogError("Failed to send two-factor code. Status: {Status}, Message: {Message}",
                    sendResult.Status, sendResult.Message);
                throw new InvalidOperationException($"Failed to send two-factor code: {sendResult.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending two-factor code");
            throw;
        }
    }
}