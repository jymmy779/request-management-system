using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        LoginRequestDto loginRequest)
    {
        var user =
            await _userManager.FindByEmailAsync(loginRequest.Email);

        if (user is null ||
            !await _userManager.CheckPasswordAsync(
                user,
                loginRequest.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid credentials",
                Detail = "Email or password is incorrect."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateToken(user, roles);

        return Ok(new LoginResponseDto
        {
            AccessToken = accessToken
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponseDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new CurrentUserResponseDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = roles.Count > 0
                ? roles[0]
                : string.Empty
        });
    }
}

