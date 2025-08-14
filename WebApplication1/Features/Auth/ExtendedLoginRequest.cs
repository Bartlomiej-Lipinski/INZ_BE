namespace WebApplication1.Features.Auth;

public class ExtendedLoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? TwoFactorCode { get; init; }
    public string? TwoFactorRecoveryCode { get; init; }
    public string? CaptchaToken { get; set; }
}