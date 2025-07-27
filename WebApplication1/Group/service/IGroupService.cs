using WebApplication1.Group.dto;

namespace WebApplication1.Group.service;

public interface IGroupService
{
    Task<Guid> CreateGroupAsync(Guid userId, GroupRequestDto request);
    Task<GroupResponseDto?> GetGroupByIdAsync(Guid id);
}