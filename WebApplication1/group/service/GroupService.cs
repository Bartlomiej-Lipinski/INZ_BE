using WebApplication1.context;
using WebApplication1.group_user;
using WebApplication1.group.dto;

namespace WebApplication1.group.service;

public class GroupService(AppDbContext context) : IGroupService
{
    public async Task<string> CreateGroupAsync(string userId, GroupRequestDto requestDto)
    {
        var group = new Group
        {
            Id = Guid.NewGuid().ToString(),
            Name = requestDto.Name,
            Color = requestDto.Color,
            Code = GenerateUniqueCode()
        };
        
        await context.Groups.AddAsync(group);

        var groupUser = new GroupUser
        {
            GroupId = group.Id,
            UserId = userId,
            IsAdmin = true
        };

        await context.GroupUsers.AddAsync(groupUser);
        await context.SaveChangesAsync();

        return group.Id;
    }

    public async Task<GroupResponseDto?> GetGroupByIdAsync(string id)
    {
        var group = await context.Groups.FindAsync(id);

        if (group == null)
            return null!;

        return new GroupResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Color = group.Color,
            Code = group.Code
        };
    }
    
    private static string GenerateUniqueCode()
    {
        return Guid.NewGuid().ToString()[..8].ToUpper();
    }
}