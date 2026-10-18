using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.CanAccessRequests)]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentService _departmentService;

    public DepartmentsController(DepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DepartmentResponseDto>>> GetAll()
    {
        var departments = await _departmentService.GetAllAsync();

        return Ok(departments);
    }
}
