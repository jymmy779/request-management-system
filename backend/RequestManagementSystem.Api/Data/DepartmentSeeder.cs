using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Data;

public static class DepartmentSeeder
{
    public const string InformationTechnologyCode = "IT";
    public const string HumanResourcesCode = "HR";
    public const string FinanceCode = "FIN";
    public const string OperationsCode = "OPS";
    public const string ProcurementCode = "PROC";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();

        var departments = new[]
        {
            new Department
            {
                Code = InformationTechnologyCode,
                Name = "Information Technology"
            },
            new Department
            {
                Code = HumanResourcesCode,
                Name = "Human Resources"
            },
            new Department
            {
                Code = FinanceCode,
                Name = "Finance"
            },
            new Department
            {
                Code = OperationsCode,
                Name = "Operations"
            },
            new Department
            {
                Code = ProcurementCode,
                Name = "Procurement"
            }
        };

        var existingDepartments = await context.Departments
            .ToDictionaryAsync(department => department.Code);

        foreach (var department in departments)
        {
            if (!existingDepartments.TryGetValue(
                    department.Code,
                    out var existingDepartment))
            {
                context.Departments.Add(department);
                continue;
            }

            existingDepartment.Name = department.Name;
        }

        await context.SaveChangesAsync();
    }
}
