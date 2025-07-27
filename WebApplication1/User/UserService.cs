using WebApplication1.User;

namespace WebApplication1.Models;

public class UserService : IUserService
{
    public Task<bool> ValidateUserAsync(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public Task<CreateUserRequest> CreateUserAsync(CreateUserRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<GetUserResponseModel> GetUserAsync(string userName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteUserAsync(string userName)
    {
        throw new NotImplementedException();
    }
}