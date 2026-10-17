using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Authorization;
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
    public async Task<IActionResult> Login(
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

        Response.Cookies.Append(
            AuthCookieNames.AccessToken,
            accessToken,
            CreateAccessTokenCookieOptions());

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(
            AuthCookieNames.AccessToken,
            CreateAccessTokenCookieOptions());

        return NoContent();
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
        var departmentName = await _userManager.Users
            .AsNoTracking()
            .Where(item => item.Id == user.Id)
            .Select(item => item.Department != null
                ? item.Department.Name
                : null)
            .SingleAsync();

        return Ok(new CurrentUserResponseDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = roles.Count > 0
                ? roles[0]
                : string.Empty,
            DepartmentName = departmentName
        });
    }

    private CookieOptions CreateAccessTokenCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
    }
}
