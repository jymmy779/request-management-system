using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Data;

namespace RequestManagementSystem.Tests;

public class DepartmentSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenCalledTwice_DoesNotDuplicateDepartments()
    {
        await using var connection =
            new SqliteConnection("DataSource=:memory:");

        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connection));

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var context =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.EnsureCreatedAsync();

        await DepartmentSeeder.SeedAsync(scope.ServiceProvider);
        await DepartmentSeeder.SeedAsync(scope.ServiceProvider);

        var departments = await context.Departments
            .OrderBy(department => department.Code)
            .ToListAsync();

        Assert.Equal(5, departments.Count);
        Assert.Equal(
            departments.Count,
            departments.Select(department => department.Code).Distinct().Count());

        Assert.Equal(
            new[] { "FIN", "HR", "IT", "OPS", "PROC" },
            departments.Select(department => department.Code));
    }
}
