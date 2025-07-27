using WebApplication1.user.dto;

namespace WebApplication1.user.service;

public class UserService : IUserService
{
    public Task<bool> ValidateUserAsync(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public Task<UserRequestDto> CreateUserAsync(UserRequestDto request)
    {
        throw new NotImplementedException();
    }

    public Task<UserResponseDto> GetUserAsync(string userName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteUserAsync(string userName)
    {
        throw new NotImplementedException();
    }
}