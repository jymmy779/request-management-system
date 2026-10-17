using Microsoft.AspNetCore.Identity;
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

        await SeedUserAsync(
            configuration,
            userManager,
            AppRoles.Employee);

        await SeedUserAsync(
            configuration,
            userManager,
            AppRoles.Manager);

        await SeedUserAsync(
            configuration,
            userManager,
            AppRoles.Admin);
    }

    private static async Task SeedUserAsync(
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        string role)
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

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
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
        else if (user.FullName != fullName)
        {
            user.FullName = fullName;

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