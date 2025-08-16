namespace WebApplication1.Features.Auth;

public class CaptchaValidationResponse
{
    public bool Success { get; set; }
    public string[] ErrorCodes { get; set; } = [];
    public double Score { get; set; }
    public string? Action { get; set; }
    public DateTime ChallengeTs { get; set; }
    public string? Hostname { get; set; }
}