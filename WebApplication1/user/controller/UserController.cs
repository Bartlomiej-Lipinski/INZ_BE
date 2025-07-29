using Microsoft.AspNetCore.Mvc;
using WebApplication1.user.service;

namespace WebApplication1.user.controller;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
   
}