using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Data;

public static class DemoUserSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var configuration =
            services.GetRequiredService<IConfiguration>();

        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        var context = services.GetRequiredService<AppDbContext>();

        await SeedUserAsync(
            configuration,
            userManager,
            context,
            AppRoles.Employee,
            DepartmentSeeder.InformationTechnologyCode);

        await SeedUserAsync(
            configuration,
            userManager,
            context,
            AppRoles.Manager,
            DepartmentSeeder.InformationTechnologyCode);

        await SeedUserAsync(
            configuration,
            userManager,
            context,
            AppRoles.Admin,
            DepartmentSeeder.OperationsCode);
    }

    private static async Task SeedUserAsync(
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        AppDbContext context,
        string role,
        string departmentCode)
    {
        var email = configuration[$"DemoUsers:{role}:Email"]
            ?? throw new InvalidOperationException(
                $"Demo {role} email not found.");

        var password = configuration[$"DemoUsers:{role}:Password"]
            ?? throw new InvalidOperationException(
                $"Demo {role} password not found.");

        var fullName =
            configuration[$"DemoUsers:{role}:FullName"]
            ?? $"Demo {role}";

        var departmentId = await context.Departments
            .Where(department => department.Code == departmentCode)
            .Select(department => department.Id)
            .SingleOrDefaultAsync();

        if (departmentId == 0)
        {
            throw new InvalidOperationException(
                $"Demo department '{departmentCode}' not found.");
        }

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                DepartmentId = departmentId
            };

            var createResult =
                await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(
                        error => error.Description));

                throw new InvalidOperationException(
                    $"Could not create demo {role}: {errors}");
            }
        }
        else if (user.FullName != fullName ||
                 user.DepartmentId != departmentId)
        {
            user.FullName = fullName;
            user.DepartmentId = departmentId;

            var updateResult =
                await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not update demo {role}.");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult =
                await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign {role} role.");
            }
        }
    }
}
