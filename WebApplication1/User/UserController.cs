using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.User;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
   [HttpPost("create")]
   public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
   {
      if (request == null)
      {
         return BadRequest("Request cannot be null.");
      }

      var result = await userService.CreateUserAsync(request);
      if (result == null)
      {
         return BadRequest("Failed to create user.");
      }

      return Ok(result);
   }
   
   [HttpGet("get/{user}")]
   public async Task<IActionResult> GetUserAsync(string user)
   {
      if (string.IsNullOrEmpty(user))
      {
         return BadRequest("User name cannot be null or empty.");
      }

      var result = await userService.GetUserAsync(user);
      if (result == null)
      {
         return NotFound("User not found.");
      }

      return Ok(result);
   }
   
}