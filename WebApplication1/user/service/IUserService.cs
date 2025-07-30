using WebApplication1.user.dto;

namespace WebApplication1.user.service;

public interface IUserService 
{
    Task<bool> ValidateUserAsync(string userName, string password);
    Task<UserResponseDto> GetUserAsync(string userName);
    Task<bool> DeleteUserAsync(string userName);
}