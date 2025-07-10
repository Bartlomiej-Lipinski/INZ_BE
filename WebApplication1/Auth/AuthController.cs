using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Auth;

[Controller]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly DBContext _dbContext;
    private readonly IAuthenticationService _authenticationService;
    
    public AuthController(UserManager<User> userManager, DBContext dbContext, IAuthenticationService authenticationService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _authenticationService = authenticationService;
    }
    
}