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
    private readonly UserDBContext _userDBContext;
    private readonly IAuthenticationService _authenticationService;
    
    public AuthController(UserManager<User> userManager, UserDBContext userDBContext, IAuthenticationService authenticationService)
    {
        _userManager = userManager;
        _userDBContext = userDBContext;
        _authenticationService = authenticationService;
    }
    
}