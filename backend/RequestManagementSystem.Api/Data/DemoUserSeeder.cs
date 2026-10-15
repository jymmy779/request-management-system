using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Data;

public static class DemoUserSeeder
{
    public static async Task SeedEmployeeAsync(
        IServiceProvider services)
    {
        var configuration =
            services.GetRequiredService<IConfiguration>();

        var email = configuration["DemoUsers:Employee:Email"]
            ?? throw new InvalidOperationException(
                "Demo Employee email not found.");

        var password =
            configuration["DemoUsers:Employee:Password"]
            ?? throw new InvalidOperationException(
                "Demo Employee password not found.");

        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
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
                    $"Could not create demo Employee: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(
                user,
                AppRoles.Employee))
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    user,
                    AppRoles.Employee);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not assign Employee role.");
            }
        }
    }
}