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

        Assert.Contains(requests, request =>
            request.Priority == RequestPriorities.Low);
        Assert.Contains(requests, request =>
            request.Priority == RequestPriorities.Medium);
        Assert.Contains(requests, request =>
            request.Priority == RequestPriorities.High);

        Assert.All(requests, request =>
            Assert.InRange(request.Quantity, 1, 5));
        Assert.Equal(5, requests.Select(
            request => request.Quantity).Distinct().Count());

        Assert.All(
            requests.Where(request =>
                request.Category == RequestCategories.Equipment ||
                request.Category == RequestCategories.Software),
            request => Assert.True(request.EstimatedCost > 0));
        Assert.Contains(requests, request =>
            request.EstimatedCost is null);
        Assert.Contains(requests, request =>
            request.EstimatedCost > 0);

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

            if (request.Status == RequestStatuses.Pending)
            {
                Assert.Null(request.UpdatedAt);
            }
            else
            {
                Assert.Equal(history[^1].ChangedAt, request.UpdatedAt);
            }

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

        var nonDemoRequest = new Request
        {
            Title = "Real request that backfill must not change",
            Description = "This row is outside the demo-load scope.",
            Category = RequestCategories.Other,
            Priority = RequestPriorities.High,
            Quantity = 9,
            EstimatedCost = 999m,
            RequesterId = employee.Id
        };

        context.Requests.Add(nonDemoRequest);

        var demoRequests = await context.Requests
            .Where(request => request.Title.StartsWith("[DEMO-LOAD]"))
            .ToListAsync();

        foreach (var request in demoRequests)
        {
            request.Priority = RequestPriorities.Medium;
            request.Quantity = 1;
            request.EstimatedCost = null;
            request.UpdatedAt = null;
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var firstUpdatedCount =
            await DemoRequestSeeder.BackfillAsync(context);
        var secondUpdatedCount =
            await DemoRequestSeeder.BackfillAsync(context);

        Assert.Equal(20, firstUpdatedCount);
        Assert.Equal(0, secondUpdatedCount);

        var savedNonDemoRequest = await context.Requests
            .AsNoTracking()
            .SingleAsync(request => request.Id == nonDemoRequest.Id);

        Assert.Equal(RequestPriorities.High, savedNonDemoRequest.Priority);
        Assert.Equal(9, savedNonDemoRequest.Quantity);
        Assert.Equal(999m, savedNonDemoRequest.EstimatedCost);
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
