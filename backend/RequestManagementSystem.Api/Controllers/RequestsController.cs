using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.CanAccessRequests)]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly RequestService _requestService;

    public RequestsController(RequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var canViewAll =
            User.IsInRole(AppRoles.Manager) ||
            User.IsInRole(AppRoles.Admin);

        var requests = await _requestService.GetAllAsync(
            userId,
            canViewAll);

        return Ok(requests);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var canViewAll =
            User.IsInRole(AppRoles.Manager) ||
            User.IsInRole(AppRoles.Admin);

        var request = await _requestService.GetByIdAsync(
            id,
            userId,
            canViewAll);

        if (request is null)
        {
            return NotFound();
        }

        return Ok(request);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateRequestDto dto)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var request = await _requestService.CreateAsync(
            dto,
            userId);

        return Created(
            $"/api/requests/{request.Id}",
            request);
    }
}
