using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Tests;

public class AuthApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string TestEmail = "employee@test.local";
    private const string TestPassword = "Test123!";

    private readonly CustomWebApplicationFactory _factory;

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WhenPasswordIsInvalid_ReturnsUnauthorized()
    {
        await CreateUserAsync(
            TestEmail,
            AppRoles.Employee);

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = "Wrong123!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_SetsHttpOnlyCookie()
    {
        await CreateUserAsync(
            TestEmail,
            AppRoles.Employee);

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = TestPassword
            });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var setCookie = Assert.Single(
            response.Headers.GetValues("Set-Cookie"));

        Assert.StartsWith(
            $"{AuthCookieNames.AccessToken}=",
            setCookie);
        Assert.Contains(
            "httponly",
            setCookie.ToLowerInvariant());
        Assert.Contains(
            "samesite=lax",
            setCookie.ToLowerInvariant());
    }

    [Fact]
    public async Task Me_WithoutAccessCookie_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Theory]
    [InlineData(TestEmail, AppRoles.Employee)]
    [InlineData("manager@test.local", AppRoles.Manager)]
    [InlineData("admin@test.local", AppRoles.Admin)]
    public async Task Me_WithValidAccessCookie_ReturnsCurrentUser(
        string email,
        string expectedRole)
    {
        await CreateUserAsync(
            email,
            expectedRole);

        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = email,
                Password = TestPassword
            });

        loginResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/auth/me");

        response.EnsureSuccessStatusCode();

        var currentUser =
            await response.Content
                .ReadFromJsonAsync<CurrentUserResponseDto>();

        Assert.NotNull(currentUser);
        Assert.Equal(email, currentUser.Email);
        Assert.Equal(expectedRole, currentUser.Role);
        Assert.Equal("Test Department", currentUser.DepartmentName);
    }

    [Fact]
    public async Task Logout_AfterLogin_RemovesAccessCookie()
    {
        await CreateUserAsync(
            TestEmail,
            AppRoles.Employee);

        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = TestPassword
            });

        loginResponse.EnsureSuccessStatusCode();

        var logoutResponse = await client.PostAsync(
            "/api/auth/logout",
            content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            meResponse.StatusCode);
    }

    private async Task CreateUserAsync(
        string email,
        string role)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
        await RoleSeeder.SeedAsync(scope.ServiceProvider);

        var department = await dbContext.Departments
            .SingleOrDefaultAsync(item => item.Code == "TEST");

        if (department is null)
        {
            department = new Department
            {
                Code = "TEST",
                Name = "Test Department"
            };

            dbContext.Departments.Add(department);
            await dbContext.SaveChangesAsync();
        }

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DepartmentId = department.Id
            };

            var createResult =
                await userManager.CreateAsync(
                    user,
                    TestPassword);

            Assert.True(
                createResult.Succeeded,
                string.Join(
                    "; ",
                    createResult.Errors.Select(
                        error => error.Description)));
        }
        else if (user.DepartmentId != department.Id)
        {
            user.DepartmentId = department.Id;

            var updateResult = await userManager.UpdateAsync(user);

            Assert.True(updateResult.Succeeded);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    user,
                    role);

            Assert.True(roleResult.Succeeded);
        }
    }
}
