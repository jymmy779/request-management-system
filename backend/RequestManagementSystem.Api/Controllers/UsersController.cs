using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserResponseDto>>> GetAll(
        [FromQuery] UserQueryParameters parameters)
    {
        var users = await _userService.GetAllAsync(parameters);

        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> Create(
        CreateUserDto input)
    {
        var result = await _userService.CreateAsync(input);

        return result.Outcome switch
        {
            UserCreationOutcome.Created => StatusCode(
                StatusCodes.Status201Created,
                result.User),
            UserCreationOutcome.Conflict => Conflict(new ProblemDetails
            {
                Title = "User already exists",
                Detail = result.Errors.FirstOrDefault()
            }),
            UserCreationOutcome.InvalidDepartment => BadRequest(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        [nameof(input.DepartmentId)] =
                            result.Errors.ToArray()
                    })),
            _ => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(input.Password)] = result.Errors.ToArray()
                }))
        };
    }
}
