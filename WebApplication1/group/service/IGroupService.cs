using WebApplication1.group.dto;

namespace WebApplication1.group.service;

public interface IGroupService
{
    Task<string> CreateGroupAsync(string userId, GroupRequestDto request);
    Task<GroupResponseDto?> GetGroupByIdAsync(string id);
}