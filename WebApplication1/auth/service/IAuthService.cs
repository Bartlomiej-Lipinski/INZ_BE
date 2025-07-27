using WebApplication1.user;

namespace WebApplication1.auth.service;

public interface IAuthService
{
    Task<(string token, string refreshToken)> GenerateTokensAsync(User user);
    Task GeneratePasswordResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}