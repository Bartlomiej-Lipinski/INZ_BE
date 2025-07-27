using WebApplication1.group.dto;

namespace WebApplication1.group.service;

public interface IGroupService
{
    Task<Guid> CreateGroupAsync(Guid userId, GroupRequestDto request);
    Task<GroupResponseDto?> GetGroupByIdAsync(Guid id);
}