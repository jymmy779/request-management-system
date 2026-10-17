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
    public async Task<IActionResult> GetAll(
    [FromQuery] RequestQueryParameters parameters)
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
            canViewAll,
            parameters);

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

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id)
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

        var history = await _requestService.GetHistoryAsync(
            id,
            userId,
            canViewAll);

        if (history is null)
        {
            return NotFound();
        }

        return Ok(history);
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

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateRequestDto dto)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _requestService.UpdateAsync(
            id,
            userId,
            dto);

        return result.Outcome switch
        {
            RequestUpdateOutcome.Success => Ok(result.Request),
            RequestUpdateOutcome.NotFound => NotFound(),
            RequestUpdateOutcome.Forbidden => Forbid(),
            RequestUpdateOutcome.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Request cannot be edited",
                detail: "The request is no longer Pending or was changed " +
                    "by another operation."),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = AppPolicies.ManagerOrAdmin)]
    public async Task<IActionResult> Approve(int id)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _requestService.ApproveAsync(
            id,
            userId);

        return result.Outcome switch
        {
            RequestTransitionOutcome.Success => Ok(result.Request),
            RequestTransitionOutcome.NotFound => NotFound(),
            RequestTransitionOutcome.Forbidden => Forbid(),
            RequestTransitionOutcome.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid request state",
                detail: "Only a Pending request can be approved."),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = AppPolicies.ManagerOrAdmin)]
    public async Task<IActionResult> Reject(
        int id,
        RejectRequestDto dto)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _requestService.RejectAsync(
            id,
            userId,
            dto);

        return result.Outcome switch
        {
            RequestTransitionOutcome.Success => Ok(result.Request),
            RequestTransitionOutcome.NotFound => NotFound(),
            RequestTransitionOutcome.Forbidden => Forbid(),
            RequestTransitionOutcome.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid request state",
                detail: "Only a Pending request can be rejected."),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("{id:int}/complete")]
    [Authorize(Policy = AppPolicies.ManagerOrAdmin)]
    public async Task<IActionResult> Complete(int id)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _requestService.CompleteAsync(
            id,
            userId);

        return result.Outcome switch
        {
            RequestTransitionOutcome.Success => Ok(result.Request),
            RequestTransitionOutcome.NotFound => NotFound(),
            RequestTransitionOutcome.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid request state",
                detail: "Only an Approved request can be completed."),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _requestService.CancelAsync(
            id,
            userId);

        return result.Outcome switch
        {
            RequestTransitionOutcome.Success => Ok(result.Request),
            RequestTransitionOutcome.NotFound => NotFound(),
            RequestTransitionOutcome.Forbidden => Forbid(),
            RequestTransitionOutcome.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid request state",
                detail: "Only a Pending request can be cancelled."),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
