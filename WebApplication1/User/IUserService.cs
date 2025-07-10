namespace WebApplication1.Models;

public interface IUserService 
{
    Task<bool> ValidateUserAsync(string userName, string password);
    Task<CreateUserRequest> CreateUserAsync(CreateUserRequest request);
    Task<GetUserResponseModel> GetUserAsync(string userName);
    Task<bool> DeleteUserAsync(string userName);
}