using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
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
        await CreateEmployeeAsync();

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = "Wrong123!"
            });

        Assert.Equal(
            System.Net.HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsAccessToken()
    {
        await CreateEmployeeAsync();

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = TestPassword
            });

        response.EnsureSuccessStatusCode();

        var loginResponse =
            await response.Content
                .ReadFromJsonAsync<LoginResponseDto>();

        Assert.NotNull(loginResponse);
        Assert.False(
            string.IsNullOrWhiteSpace(loginResponse.AccessToken));
        Assert.Equal("Bearer", loginResponse.TokenType);
    }

    [Fact]
    public async Task Me_WithoutAccessToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidAccessToken_ReturnsCurrentUser()
    {
        await CreateEmployeeAsync();

        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = TestPassword
            });

        loginResponse.EnsureSuccessStatusCode();

        var loginResult =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponseDto>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);

        var response = await client.GetAsync("/api/auth/me");

        response.EnsureSuccessStatusCode();

        var currentUser =
            await response.Content
                .ReadFromJsonAsync<CurrentUserResponseDto>();

        Assert.NotNull(currentUser);
        Assert.Equal(TestEmail, currentUser.Email);
        Assert.Equal(AppRoles.Employee, currentUser.Role);
    }

    private async Task CreateEmployeeAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
        await RoleSeeder.SeedAsync(scope.ServiceProvider);

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var existingUser =
            await userManager.FindByEmailAsync(TestEmail);

        if (existingUser is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = TestEmail,
            Email = TestEmail,
            EmailConfirmed = true
        };

        var createResult =
            await userManager.CreateAsync(user, TestPassword);

        Assert.True(
            createResult.Succeeded,
            string.Join(
                "; ",
                createResult.Errors.Select(
                    error => error.Description)));

        var roleResult =
            await userManager.AddToRoleAsync(
                user,
                AppRoles.Employee);

        Assert.True(roleResult.Succeeded);
    }
}