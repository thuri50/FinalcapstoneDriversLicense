
using FinalcapstoneDriversLicense.Dtos.Auth;
using FinalcapstoneDriversLicense.Entities;
using FinalcapstoneDriversLicense.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DriversLicenseApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, ILogger<AuthController> logger)
    {
        _authService = authService;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        => Ok(await _authService.LoginAsync(request));
    [HttpPost("register-staff")]
    [Authorize(Roles = "Admin")] // Optional: Restrict so only existing Admins can create staff accounts
    public async Task<ActionResult<AuthResponse>> RegisterStaff([FromBody] RegisterRequest request)
    {
        // 1. Check if user already exists
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return BadRequest(new { message = "An account with this email already exists." });

        // 2. Create the ApplicationUser entity
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });

        // 3. Assign the "Staff" role (Ensure the role exists first)
        if (!await _roleManager.RoleExistsAsync("Staff"))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>("Staff"));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Staff");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(new { message = "Could not assign Staff role." });
        }

        _logger.LogInformation("Staff account created for {Email}", user.Email);

        // 4. Return token response (same format as login/register)
        var response =  _authService.CreateTokenAsync(user);
        return Ok(response); ;
    }
}
