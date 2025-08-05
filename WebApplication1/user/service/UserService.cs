using Microsoft.EntityFrameworkCore;
using WebApplication1.context;
using WebApplication1.user.dto;

namespace WebApplication1.user.service;

public class UserService(AppDbContext context) : IUserService
{
    public Task<bool> ValidateUserAsync(string userName, string password)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("User name and password cannot be null or empty.");
        }

        return context.Users
            .AnyAsync(u => u.UserName == userName && u.PasswordHash == password);
    }

    //metoda tworząca użytkownika przeniesona do AuthController, ponieważ jest to część procesu logowania i rejestracji

    public Task<UserResponseDto> GetUserAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("User name cannot be null or empty.", nameof(id));
        }

        var user = context.Users
            .Where(u => u.Id == id)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Name = u.Name,
                Surname = u.Surname
            })
            .FirstOrDefaultAsync();

        return user;
    }

    public Task<bool> DeleteUserAsync(string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentException("User name cannot be null or empty.", nameof(userName));
        }

        var user = context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user == null)
        {
            return Task.FromResult(false);
        }

        context.Users.Remove(user.Result);
        return context.SaveChangesAsync().ContinueWith(t => t.Result > 0);
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