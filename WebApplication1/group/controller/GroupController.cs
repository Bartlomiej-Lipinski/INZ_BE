using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.group.dto;
using WebApplication1.group.service;

namespace WebApplication1.group.controller;

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

        var newGroupId = await groupService.CreateGroupAsync(userGuid.ToString(), requestDto);
        if (newGroupId == string.Empty)
        {
            return BadRequest("Failed to create group.");
        }
        
        return CreatedAtAction(nameof(GetGroupById), new { id = newGroupId }, new { Id = newGroupId });

    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupResponseDto?>> GetGroupById(string id)
    {
        var group = await groupService.GetGroupByIdAsync(id);
        if (group == null)
            return NotFound();
        return Ok(group);
    }
}