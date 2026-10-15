using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;

namespace RequestManagementSystem.Api.Data;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roleNames =
        {
            AppRoles.Employee,
            AppRoles.Manager,
            AppRoles.Admin
        };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result =
                    await roleManager.CreateAsync(new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Không thể tạo role '{roleName}'.");
                }
            }
        }
    }
}