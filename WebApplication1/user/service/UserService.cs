using Microsoft.EntityFrameworkCore;
using WebApplication1.context;
using WebApplication1.user.dto;

namespace WebApplication1.user.service;

public class UserService(AppDbContext context) : IUserService
{
    public Task<bool> ValidateUserAsync(string userName, string password)
    {
        throw new NotImplementedException();
    }

    //metoda tworząca użytkownika przeniesona do AuthController, ponieważ jest to część procesu logowania i rejestracji

    public Task<UserResponseDto> GetUserAsync(string userName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteUserAsync(string userName)
    {
        throw new NotImplementedException();
    }

    public Task<List<UserResponseDto>> GetAllUsersAsync()
    {
        var users = context.Users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            Name = u.Name,
            Surname = u.Surname
        }).ToListAsync();

        return users;
    }
}