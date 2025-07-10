using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Models;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    
}