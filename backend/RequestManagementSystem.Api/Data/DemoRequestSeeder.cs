using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Data;

public static class DemoRequestSeeder
{
    private const string DemoTitlePrefix = "[DEMO-LOAD]";
    private const int BatchSize = 500;

    private static readonly string[] Titles =
    [
        "Request a new monitor",
        "Request software license",
        "Replace broken keyboard",
        "Install development tools",
        "Request office equipment"
    ];

    private static readonly string[] Categories =
    [
        RequestCategories.Equipment,
        RequestCategories.Software,
        RequestCategories.Other
    ];

    public static async Task<int> SeedAsync(
        AppDbContext context,
        IConfiguration configuration,
        int requestCount)
    {
        if (requestCount is < 1 or > 50_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestCount),
                "Request count must be between 1 and 50,000.");
        }

        if (await context.Requests.AnyAsync(
                request => request.Title.StartsWith(DemoTitlePrefix)))
        {
            return 0;
        }

        var employeeEmail = GetDemoEmail(
            configuration,
            AppRoles.Employee);

        var managerEmail = GetDemoEmail(
            configuration,
            AppRoles.Manager);

        var adminEmail = GetDemoEmail(
            configuration,
            AppRoles.Admin);

        var demoEmails = new[]
        {
            employeeEmail,
            managerEmail,
            adminEmail
        };

        var users = await context.Users
            .AsNoTracking()
            .Where(user =>
                user.Email != null &&
                demoEmails.Contains(user.Email))
            .ToListAsync();

        if (users.Count != demoEmails.Length)
        {
            throw new InvalidOperationException(
                "All three demo users must exist before seeding requests.");
        }

        var employeeId = users.Single(
            user => user.Email == employeeEmail).Id;

        var managerId = users.Single(
            user => user.Email == managerEmail).Id;

        var adminId = users.Single(
            user => user.Email == adminEmail).Id;

        var ownerIds = new[]
        {
            employeeId,
            managerId,
            adminId
        };

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        for (var number = 1; number <= requestCount; number++)
        {
            var ownerId =
                ownerIds[(number - 1) % ownerIds.Length];

            var actorId =
                ownerId == managerId
                    ? adminId
                    : managerId;

            context.Requests.Add(
                CreateRequest(
                    number,
                    ownerId,
                    actorId));

            if (number % BatchSize == 0)
            {
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }
        }

        if (requestCount % BatchSize != 0)
        {
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }

        await transaction.CommitAsync();

        return requestCount;
    }

    private static Request CreateRequest(
        int number,
        string ownerId,
        string actorId)
    {
        var status = GetStatus(number);

        var createdAt = DateTime.UtcNow
            .AddDays(-1)
            .AddMinutes(-number * 10);

        var request = new Request
        {
            Title =
                $"{DemoTitlePrefix} {number:D5} - " +
                Titles[(number - 1) % Titles.Length],
            Description =
                $"Generated demo request number {number}.",
            Category =
                Categories[(number - 1) % Categories.Length],
            Status = status,
            RequesterId = ownerId,
            CreatedAt = createdAt
        };

        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = null,
            ToStatus = RequestStatuses.Pending,
            ChangedByUserId = ownerId,
            ChangedAt = createdAt
        });

        AddFinalStatusHistory(
            request,
            status,
            ownerId,
            actorId,
            createdAt);

        return request;
    }

    private static void AddFinalStatusHistory(
        Request request,
        string status,
        string ownerId,
        string actorId,
        DateTime createdAt)
    {
        if (status == RequestStatuses.Pending)
        {
            return;
        }

        if (status == RequestStatuses.Completed)
        {
            request.StatusHistory.Add(new RequestStatusHistory
            {
                FromStatus = RequestStatuses.Pending,
                ToStatus = RequestStatuses.Approved,
                ChangedByUserId = actorId,
                ChangedAt = createdAt.AddMinutes(10)
            });

            request.StatusHistory.Add(new RequestStatusHistory
            {
                FromStatus = RequestStatuses.Approved,
                ToStatus = RequestStatuses.Completed,
                ChangedByUserId = actorId,
                ChangedAt = createdAt.AddMinutes(20)
            });

            return;
        }

        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = RequestStatuses.Pending,
            ToStatus = status,
            ChangedByUserId =
                status == RequestStatuses.Cancelled
                    ? ownerId
                    : actorId,
            Note =
                status == RequestStatuses.Rejected
                    ? "Generated rejection reason."
                    : null,
            ChangedAt = createdAt.AddMinutes(10)
        });
    }

    private static string GetStatus(int number)
    {
        return (number % 20) switch
        {
            <= 7 => RequestStatuses.Pending,
            <= 12 => RequestStatuses.Approved,
            <= 15 => RequestStatuses.Rejected,
            <= 18 => RequestStatuses.Completed,
            _ => RequestStatuses.Cancelled
        };
    }

    private static string GetDemoEmail(
        IConfiguration configuration,
        string role)
    {
        return configuration[$"DemoUsers:{role}:Email"]
            ?? throw new InvalidOperationException(
                $"Demo {role} email not found.");
    }
}