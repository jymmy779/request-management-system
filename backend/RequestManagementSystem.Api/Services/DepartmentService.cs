using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;

namespace RequestManagementSystem.Api.Services;

public class DepartmentService
{
    private readonly AppDbContext _context;

    public DepartmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentResponseDto>> GetAllAsync()
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentResponseDto
            {
                Id = department.Id,
                Code = department.Code,
                Name = department.Name
            })
            .ToListAsync();
    }
}
