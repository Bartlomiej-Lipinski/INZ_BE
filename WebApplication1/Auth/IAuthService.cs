namespace WebApplication1.Auth;

public interface IAuthService
{
    Task<(string token, string refreshToken)> GenerateTokensAsync(User.User user);
    Task GeneratePasswordResetTokenAsync(string email);
}