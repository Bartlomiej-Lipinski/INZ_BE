using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Group.dto;
using WebApplication1.Group.service;

namespace WebApplication1.Group.controller;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupController(IGroupService groupService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateGroup([FromBody] GroupRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userGuid))
        {
            return Unauthorized();
        }

        var newGroupId = await groupService.CreateGroupAsync(userGuid, requestDto);
        if (newGroupId == Guid.Empty)
        {
            return BadRequest("Failed to create group.");
        }
        
        return CreatedAtAction(nameof(GetGroupById), new { id = newGroupId }, new { Id = newGroupId });

    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupResponseDto?>> GetGroupById(Guid id)
    {
        var group = await groupService.GetGroupByIdAsync(id);
        if (group == null)
            return NotFound();
        return Ok(group);
    }
}