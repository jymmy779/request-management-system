using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Tests;

public class DemoRequestSeederTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DemoRequestSeederTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedAsync_CreatesConsistentDataAndDoesNotDuplicate()
    {
        using var scope = _factory.Services.CreateScope();

        var context =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var employee = CreateUser(
            "seed-employee@test.local",
            "Seed Employee");

        var manager = CreateUser(
            "seed-manager@test.local",
            "Seed Manager");

        var admin = CreateUser(
            "seed-admin@test.local",
            "Seed Admin");

        context.Users.AddRange(employee, manager, admin);
        await context.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"DemoUsers:{AppRoles.Employee}:Email"] = employee.Email,
                [$"DemoUsers:{AppRoles.Manager}:Email"] = manager.Email,
                [$"DemoUsers:{AppRoles.Admin}:Email"] = admin.Email
            })
            .Build();

        var firstInsertedCount = await DemoRequestSeeder.SeedAsync(
            context,
            configuration,
            20);

        var secondInsertedCount = await DemoRequestSeeder.SeedAsync(
            context,
            configuration,
            20);

        Assert.Equal(20, firstInsertedCount);
        Assert.Equal(0, secondInsertedCount);

        var requests = await context.Requests
            .AsNoTracking()
            .Include(request => request.StatusHistory)
            .ToListAsync();

        Assert.Equal(20, requests.Count);
        Assert.Equal(35, requests.Sum(
            request => request.StatusHistory.Count));

        Assert.Equal(8, requests.Count(
            request => request.Status == RequestStatuses.Pending));
        Assert.Equal(5, requests.Count(
            request => request.Status == RequestStatuses.Approved));
        Assert.Equal(3, requests.Count(
            request => request.Status == RequestStatuses.Rejected));
        Assert.Equal(3, requests.Count(
            request => request.Status == RequestStatuses.Completed));
        Assert.Single(
            requests,
            request => request.Status == RequestStatuses.Cancelled);

        foreach (var request in requests)
        {
            var history = request.StatusHistory
                .OrderBy(entry => entry.ChangedAt)
                .ThenBy(entry => entry.Id)
                .ToList();

            Assert.Equal(
                GetExpectedHistoryCount(request.Status),
                history.Count);

            Assert.Null(history[0].FromStatus);
            Assert.Equal(
                RequestStatuses.Pending,
                history[0].ToStatus);
            Assert.Equal(request.Status, history[^1].ToStatus);

            if (request.Status == RequestStatuses.Rejected)
            {
                Assert.False(string.IsNullOrWhiteSpace(
                    history[^1].Note));
            }

            if (request.Status is RequestStatuses.Approved
                or RequestStatuses.Rejected)
            {
                Assert.NotEqual(
                    request.RequesterId,
                    history[^1].ChangedByUserId);
            }

            if (request.Status == RequestStatuses.Cancelled)
            {
                Assert.Equal(
                    request.RequesterId,
                    history[^1].ChangedByUserId);
            }
        }
    }

    private static ApplicationUser CreateUser(
        string email,
        string fullName)
    {
        return new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };
    }

    private static int GetExpectedHistoryCount(string status)
    {
        return status switch
        {
            RequestStatuses.Pending => 1,
            RequestStatuses.Approved => 2,
            RequestStatuses.Rejected => 2,
            RequestStatuses.Completed => 3,
            RequestStatuses.Cancelled => 2,
            _ => throw new InvalidOperationException(
                $"Unexpected request status: {status}")
        };
    }
}
