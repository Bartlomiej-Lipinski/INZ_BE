using System.Text.Json;

namespace WebApplication1.Features.Auth.Services;

public interface ICaptchaService
{
    Task<bool> ValidateCaptchaAsync(string captchaToken, string userIpAddress);
}

internal sealed class CaptchaService(HttpClient httpClient, IConfiguration configuration, ILogger<CaptchaService> logger) 
    : ICaptchaService
{
    public async Task<bool> ValidateCaptchaAsync(string captchaToken, string userIpAddress)
        {
            try
            {
                var secretKey = configuration["ReCaptcha:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    logger.LogError("ReCaptcha SecretKey not configured");
                    return false;
                }

                var parameters = new Dictionary<string, string>
                {
                    {"secret", secretKey},
                    {"response", captchaToken},
                    {"remoteip", userIpAddress}
                };

                var encodedContent = new FormUrlEncodedContent(parameters);
                var apiUrl = configuration["ReCaptcha:ApiUrl"] ?? "https://www.google.com/recaptcha/api/siteverify";
                var response = await httpClient.PostAsync(apiUrl, encodedContent);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("ReCaptcha API request failed with status: {StatusCode}", response.StatusCode);
                    return false;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var captchaResponse = JsonSerializer.Deserialize<CaptchaValidationResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (captchaResponse?.Success == true)
                {
                    var minScore = configuration.GetValue("ReCaptcha:MinScore", 0.5);
                    if (captchaResponse.Score >= minScore)
                    {
                        return true;
                    }

                    logger.LogWarning("ReCaptcha score too low: {Score}", captchaResponse.Score);
                    return false;
                }

                logger.LogWarning("ReCaptcha validation failed. Errors: {Errors}", 
                    string.Join(", ", captchaResponse?.ErrorCodes ?? []));
                
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating ReCaptcha");
                return false;
            }
        }
    }