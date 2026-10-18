using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.Tests;

public class DepartmentsApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DepartmentsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutAccessToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/departments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithRecognizedRole_ReturnsDepartmentsSortedByName()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Departments.AddRange(
            new Department { Code = "OPS", Name = "Operations" },
            new Department { Code = "FIN", Name = "Finance" });

        await dbContext.SaveChangesAsync();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "departments-user@test.local",
            Email = "departments-user@test.local"
        };

        var accessToken = tokenService.CreateToken(
            user,
            new[] { AppRoles.Employee });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/departments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var departments = await response.Content
            .ReadFromJsonAsync<List<DepartmentResponseDto>>();

        Assert.NotNull(departments);
        Assert.Equal(2, departments.Count);
        Assert.Equal("FIN", departments[0].Code);
        Assert.Equal("Finance", departments[0].Name);
        Assert.Equal("OPS", departments[1].Code);
        Assert.Equal("Operations", departments[1].Name);
    }
}
