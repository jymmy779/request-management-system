using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.Tests;

public class UsersApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutAccessToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Employee)]
    [InlineData(AppRoles.Manager)]
    public async Task GetAll_WithoutAdminRole_ReturnsForbidden(string role)
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(scenario.Admin, role);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAdminRole_ReturnsFilteredPagedUsers()
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(
            scenario.Admin,
            AppRoles.Admin);

        var response = await client.GetAsync(
            $"/api/users?role=Employee" +
            $"&departmentId={scenario.EngineeringDepartmentId}" +
            "&page=1&pageSize=1" +
            "&sortBy=fullName&sortDirection=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<UserResponseDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Equal("Alice Employee", result.Items[0].FullName);
        Assert.Equal("alice@test.local", result.Items[0].Email);
        Assert.Equal(AppRoles.Employee, result.Items[0].Role);
        Assert.Equal("Engineering", result.Items[0].DepartmentName);
    }

    [Fact]
    public async Task GetAll_WithInvalidRoleFilter_ReturnsBadRequest()
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(
            scenario.Admin,
            AppRoles.Admin);

        var response = await client.GetAsync("/api/users?role=Owner");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAdminRole_HashesPasswordAndAssignsRoleAndDepartment()
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(
            scenario.Admin,
            AppRoles.Admin);
        var password = "Valid!Password123";

        var response = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserDto
            {
                FullName = "Created Employee",
                Email = "created@test.local",
                Password = password,
                Role = AppRoles.Employee,
                DepartmentId = scenario.EngineeringDepartmentId
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content
            .ReadFromJsonAsync<UserResponseDto>();

        Assert.NotNull(created);
        Assert.Equal("Created Employee", created.FullName);
        Assert.Equal(AppRoles.Employee, created.Role);
        Assert.Equal("Engineering", created.DepartmentName);

        using var scope = _factory.Services.CreateScope();
        var userManager =
            scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>>();

        var persisted = await userManager.FindByEmailAsync(
            "created@test.local");

        Assert.NotNull(persisted);
        Assert.NotEqual(password, persisted.PasswordHash);
        Assert.True(await userManager.CheckPasswordAsync(
            persisted,
            password));
        Assert.True(await userManager.IsInRoleAsync(
            persisted,
            AppRoles.Employee));
    }

    [Fact]
    public async Task Create_WithManagerRole_ReturnsForbidden()
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(
            scenario.Admin,
            AppRoles.Manager);

        var response = await client.PostAsJsonAsync(
            "/api/users",
            CreateValidInput(scenario.EngineeringDepartmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithWeakPassword_ReturnsBadRequestAndPersistsNothing()
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(
            scenario.Admin,
            AppRoles.Admin);
        var input = CreateValidInput(scenario.EngineeringDepartmentId);
        input.Password = "weak";

        var response = await client.PostAsJsonAsync("/api/users", input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager =
            scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>>();

        Assert.Null(await userManager.FindByEmailAsync(input.Email));
    }

    [Fact]
    public async Task Create_WithExistingEmail_ReturnsConflict()
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(
            scenario.Admin,
            AppRoles.Admin);
        var input = CreateValidInput(scenario.EngineeringDepartmentId);
        input.Email = "alice@test.local";

        var response = await client.PostAsJsonAsync("/api/users", input);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingDepartment_ReturnsBadRequest()
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(
            scenario.Admin,
            AppRoles.Admin);
        var input = CreateValidInput(999_999);

        var response = await client.PostAsJsonAsync("/api/users", input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<UserScenario> CreateScenarioAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var engineering = new Department
        {
            Code = "ENG",
            Name = "Engineering"
        };
        var finance = new Department
        {
            Code = "FIN",
            Name = "Finance"
        };

        dbContext.Departments.AddRange(engineering, finance);
        await dbContext.SaveChangesAsync();

        var employeeRole = CreateRole(AppRoles.Employee);
        var managerRole = CreateRole(AppRoles.Manager);
        var adminRole = CreateRole(AppRoles.Admin);

        dbContext.Roles.AddRange(employeeRole, managerRole, adminRole);

        var admin = CreateUser(
            "admin@test.local",
            "Admin User",
            finance.Id);
        var alice = CreateUser(
            "alice@test.local",
            "Alice Employee",
            engineering.Id);
        var bob = CreateUser(
            "bob@test.local",
            "Bob Employee",
            engineering.Id);
        var manager = CreateUser(
            "manager@test.local",
            "Manager User",
            engineering.Id);

        dbContext.Users.AddRange(admin, alice, bob, manager);
        await dbContext.SaveChangesAsync();

        dbContext.UserRoles.AddRange(
            CreateUserRole(admin.Id, adminRole.Id),
            CreateUserRole(alice.Id, employeeRole.Id),
            CreateUserRole(bob.Id, employeeRole.Id),
            CreateUserRole(manager.Id, managerRole.Id));

        await dbContext.SaveChangesAsync();

        return new UserScenario(admin, engineering.Id);
    }

    private HttpClient CreateAuthenticatedClient(
        ApplicationUser user,
        string role)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            user,
            new[] { role });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    private static IdentityRole CreateRole(string name)
    {
        return new IdentityRole
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant()
        };
    }

    private static ApplicationUser CreateUser(
        string email,
        string fullName,
        int departmentId)
    {
        return new ApplicationUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FullName = fullName,
            DepartmentId = departmentId
        };
    }

    private static IdentityUserRole<string> CreateUserRole(
        string userId,
        string roleId)
    {
        return new IdentityUserRole<string>
        {
            UserId = userId,
            RoleId = roleId
        };
    }

    private static CreateUserDto CreateValidInput(int departmentId)
    {
        return new CreateUserDto
        {
            FullName = "New Employee",
            Email = "new-employee@test.local",
            Password = "Valid!Password123",
            Role = AppRoles.Employee,
            DepartmentId = departmentId
        };
    }

    private sealed record UserScenario(
        ApplicationUser Admin,
        int EngineeringDepartmentId);
}
