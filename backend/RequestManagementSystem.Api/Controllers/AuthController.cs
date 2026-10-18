using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    private readonly IEmailSender _emailSender;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        IEmailSender emailSender,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _frontendOptions = frontendOptions.Value;
        _logger = logger;
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

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordDto input,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(
            input.Email.Trim());

        if (user is not null &&
            user.Email is not null &&
            await _userManager.IsEmailConfirmedAsync(user))
        {
            try
            {
                var token = await _userManager
                    .GeneratePasswordResetTokenAsync(user);

                var encodedToken = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(token));

                var resetUrl = QueryHelpers.AddQueryString(
                    $"{_frontendOptions.BaseUrl.TrimEnd('/')}/reset-password",
                    new Dictionary<string, string?>
                    {
                        ["email"] = user.Email,
                        ["token"] = encodedToken
                    });

                var encodedResetUrl = HtmlEncoder.Default.Encode(
                    resetUrl);

                await _emailSender.SendAsync(
                    user.Email,
                    "Reset your Request Management System password",
                    $"""
                    <p>A password reset was requested for your account.</p>
                    <p><a href="{encodedResetUrl}">Reset password</a></p>
                    <p>If you did not request this, you can ignore this email.</p>
                    """,
                    cancellationToken);
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException)
            {
                _logger.LogError(
                    exception,
                    "Could not send a password reset email for user {UserId}.",
                    user.Id);
            }
        }

        return Accepted();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordDto input)
    {
        var user = await _userManager.FindByEmailAsync(
            input.Email.Trim());

        if (user is null)
        {
            return InvalidResetLink();
        }

        string token;

        try
        {
            token = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(input.Token));
        }
        catch (FormatException)
        {
            return InvalidResetLink();
        }

        var result = await _userManager.ResetPasswordAsync(
            user,
            token,
            input.NewPassword);

        if (!result.Succeeded)
        {
            var hasInvalidToken = result.Errors.Any(
                error => error.Code == "InvalidToken");

            if (hasInvalidToken)
            {
                return InvalidResetLink();
            }

            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(input.NewPassword)] = result.Errors
                        .Select(error => error.Description)
                        .ToArray()
                }));
        }

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
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = roles.Count > 0
                ? roles[0]
                : string.Empty,
            DepartmentName = departmentName
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordDto input)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        if (await _userManager.CheckPasswordAsync(
            user,
            input.NewPassword))
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(input.NewPassword)] =
                        ["The new password must differ from the current password."]
                }));
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            input.CurrentPassword,
            input.NewPassword);

        if (!result.Succeeded)
        {
            var passwordMismatch = result.Errors.Any(
                error => error.Code == "PasswordMismatch");

            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [passwordMismatch
                        ? nameof(input.CurrentPassword)
                        : nameof(input.NewPassword)] = result.Errors
                            .Select(error => error.Description)
                            .ToArray()
                }));
        }

        Response.Cookies.Delete(
            AuthCookieNames.AccessToken,
            CreateAccessTokenCookieOptions());

        return NoContent();
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

    private static BadRequestObjectResult InvalidResetLink()
    {
        return new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid password reset",
            Detail = "The password reset link is invalid or has expired."
        });
    }
}
