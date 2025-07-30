using WebApplication1.context;
using WebApplication1.user.dto;

namespace WebApplication1.user.service;

public class UserService(AppDbContext context) : IUserService
{
    public Task<bool> ValidateUserAsync(string userName, string password)
    {
        throw new NotImplementedException();
    }

    public Task<UserRequestDto> CreateUserAsync(UserRequestDto request)
    {
        var userRequest = new UserRequestDto()
        {
            Name = request.Name,
            Surname = request.Surname,
            Email = request.Email,
            BirthDate = request.BirthDate,
            Password = request.Password
        };
        var emailExists = context.Users.Any(u => u.Email == userRequest.Email);
        if (emailExists)
        {
            throw new ArgumentException("Email already exists.");
        }
        if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Surname) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            throw new ArgumentException("All fields are required.");
        }

        return Task.FromResult(userRequest);
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