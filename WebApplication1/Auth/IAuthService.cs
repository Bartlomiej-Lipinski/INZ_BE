using WebApplication1.Models;

namespace WebApplication1.Auth;

public interface IAuthService
{
    Task<(string token, string refreshToken)> GenerateTokensAsync(User user);    
}