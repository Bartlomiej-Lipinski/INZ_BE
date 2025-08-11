using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Tests;

public static class TestDataFactory
{
    public static Group CreateGroup(string? id = null, string? name = null, string? color = null, string? code = null)
    {
        return new Group
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name ?? "Test Group",
            Color = color ?? "#FFFFFF",
            Code = code ?? GenerateUniqueCode()
        };
    }
    
    public static GroupUser CreateGroupUser(string? userId = null, string? groupId = null, bool isAdmin = false)
    {
        return new GroupUser
        {
            UserId = userId ?? Guid.NewGuid().ToString(),
            GroupId = groupId ?? Guid.NewGuid().ToString(),
            IsAdmin = isAdmin
        };
    }

    private static string GenerateUniqueCode()
    {
        return Guid.NewGuid().ToString()[..8].ToUpper();
    }
    
    public static PostGroup.GroupRequestDto CreateGroupRequestDto(string? name = null, string? color = null)
    {
        return new PostGroup.GroupRequestDto
        {
            Name = name ?? "TestGroup",
            Color = color ?? "Red"
        };
    }
}