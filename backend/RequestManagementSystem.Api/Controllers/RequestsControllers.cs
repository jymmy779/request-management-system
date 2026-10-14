using Microsoft.AspNetCore.Mvc;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.Api.Controllers;

[ApiController]
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
        return Ok(await _requestService.GetAllAsync());
    }
}